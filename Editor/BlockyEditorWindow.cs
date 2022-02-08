using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace PeartreeGames.BlockyWorldEditor.Editor
{
    public class BlockyEditorWindow : EditorWindow
    {
        private BlockyEditorSettings _settings;
        private SerializedObject _serializedSettings;
        private BlockyObjectMap _map;
        private bool _isDragging;
        private readonly HashSet<Vector3Int> _draggingSet = new();

        private bool _isSquareDragging;
        private readonly List<Vector3Int> _draggingSquareList = new();
        private Vector3Int _dragStartPosition;
        private IBlockyPiece CurrentBlocky => _settings == null ? null : _settings.Selected;

        private GameObject _placementObject;
        private Shader _placementShader;
        

        [MenuItem("Tools/BlockyEditor")]
        private static void ShowWindow()
        {
            var window = GetWindow<BlockyEditorWindow>();
            window.titleContent = new GUIContent("BlockyWorld");
            window.Show();
        }

        private void OnEnable()
        {
            EditorSceneManager.sceneClosing += OnSceneClosing;
            _placementShader = Shader.Find("Blocky/Placement");
            PopulateMap();
            if (_settings == null)
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(BlockyEditorSettings)}");
                if (guids.Length == 0)
                {
                    _settings = CreateInstance<BlockyEditorSettings>();
                    AssetDatabase.CreateAsset(_settings, "Assets/BlockyEditorSettings.asset");
                    var defaultParent = CreateInstance<BlockyDefaultParentSetter>();
                    defaultParent.name = defaultParent.GetType().Name;
                    AssetDatabase.AddObjectToAsset(defaultParent, _settings);
                    _settings.parentSetter = defaultParent;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_settings));
                }
                else
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _settings = AssetDatabase.LoadAssetAtPath<BlockyEditorSettings>(path);
                }
            }

            if (_settings.parentSetter == null)
            {
                _settings.parentSetter =
                    AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_settings))
                        .First(a => a is BlockyParentSetter) as BlockyParentSetter;
                
            }

            _settings.editMode = BlockyEditMode.None;
            _serializedSettings ??= new SerializedObject(_settings);
            _serializedSettings.ApplyModifiedProperties();
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("BlockyEditor"));
            var settingsView = new BlockySettingsElement(this, _serializedSettings);
            rootVisualElement.Add(settingsView);

            RefreshPalette();
            Repaint();
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneClosing -= OnSceneClosing;
        }

        private void OnSceneClosing(Scene scene, bool removingScene)
        {
            if (_settings.editMode == BlockyEditMode.None) return;
            // In Painting/Selection mode while scene is closing, process the scenes beforehand
            var preprocessors = GetScenePreprocessors();
            foreach(var preprocessor in preprocessors) preprocessor.ProcessScene(this, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public void OnBlockyModeChange(BlockyEditMode mode)
        {
            var preprocessors = GetScenePreprocessors();
            var openSceneCount = SceneManager.sceneCount;
            if (mode == BlockyEditMode.None)
            {
                if (_placementObject != null) DestroyImmediate(_placementObject);
                for (var i = 0; i < openSceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    foreach(var preprocessor in preprocessors) preprocessor.ProcessScene(this, scene);
                }
                EditorSceneManager.MarkAllScenesDirty();
                EditorSceneManager.SaveOpenScenes();
            }
            else
            {
                for (var i = 0; i < openSceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    foreach (var preprocessor in preprocessors) preprocessor.RevertScene(this, scene);
                }
            }
        }

        private static List<IBlockyScenePreprocessor> GetScenePreprocessors()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var results = new List<IBlockyScenePreprocessor>();
            foreach (var assembly in assemblies)
            {
                var preprocessors = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && t.GetInterfaces().Contains(typeof(IBlockyScenePreprocessor))).Select(t => (IBlockyScenePreprocessor) Activator.CreateInstance(t)).ToList();
                results.AddRange(preprocessors);
            }
            return results;
        }


        private void OnFocus()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDestroy()
        {
            if (_placementObject != null) DestroyImmediate(_placementObject);
            SceneView.duringSceneGui -= OnSceneGUI;
            _draggingSet.Clear();
            _draggingSet.TrimExcess();
            _draggingSquareList.Clear();
            _draggingSquareList.TrimExcess();
        }

        private void OnLostFocus()
        {
            if (_placementObject != null) DestroyImmediate(_placementObject);
            _draggingSet.Clear();
            _draggingSet.TrimExcess();
            _draggingSquareList.Clear();
            _draggingSquareList.TrimExcess();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_settings.editMode == BlockyEditMode.None) return;
            if (!BlockyEditorUtilities.TryGetTargetPoint(Event.current.mousePosition, _settings.gridHeight,
                    out _settings.target)) return;
            BlockyEditorUtilities.SetTargetVisualization(_settings.target, _settings.editMode, _settings.brushSize,
                _isSquareDragging, _draggingSquareList, _dragStartPosition);
            BlockyEditorUtilities.SetPlacementVisualization(_settings.target, _settings.rotation, _settings.editMode, CurrentBlocky, _placementShader, ref _placementObject);
            _settings.parentSetter.SetVisualization(_settings.target, _settings.gridHeight);
            
            HandleInput(_settings.target);
            HandleUtility.Repaint();
        }

        private void HandleInput(Vector3Int target)
        {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.S:
                        _settings.gridHeight--;
                        break;
                    case KeyCode.W:
                        _settings.gridHeight++;
                        break;
                    case KeyCode.Q:
                        _settings.rotation.y = BlockyUtilities.ClampAngle(_settings.rotation.y + 90);
                        break;
                    case KeyCode.E:
                        _settings.rotation.y = BlockyUtilities.ClampAngle(_settings.rotation.y - 90);
                        break;
                    case KeyCode.R:
                        _settings.randomRotation = !_settings.randomRotation;
                        break;
                    case KeyCode.Equals:
                        _settings.brushSize = Mathf.Clamp(_settings.brushSize + 1, 0, 3);
                        break;
                    case KeyCode.Minus:
                        _settings.brushSize = Mathf.Clamp(_settings.brushSize - 1, 0, 3);
                        break;
                    case KeyCode.Alpha1:
                        _settings.brushSize = 0;
                        break;
                    case KeyCode.Alpha2:
                        _settings.brushSize = 1;
                        break;
                    case KeyCode.Alpha3:
                        _settings.brushSize = 2;
                        break;
                    case KeyCode.Alpha4:
                        _settings.brushSize = 3;
                        break;
                }

                evt.Use();
                Repaint();
                return;
            }

            if (evt.type == EventType.Layout)
            {
                var controlId = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);
                HandleUtility.AddDefaultControl(controlId);
            }

            Action<Vector3Int> action = _settings.editMode switch
            {
                BlockyEditMode.None => null,
                BlockyEditMode.Paint => evt.control ? RemoveBlockyObject : AddBlockyObject,
                BlockyEditMode.Select => evt.control ? RemoveBlockySelection : AddBlockySelection,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (action == null || evt.button != decimal.Zero || CurrentBlocky == null) return;

            if (evt.type == EventType.MouseUp)
            {
                evt.Use();
                if (_isSquareDragging)
                {
                    _isSquareDragging = false;
                    _dragStartPosition = Vector3Int.zero;
                    foreach (var pos in _draggingSquareList) action(pos);
                    _draggingSquareList.Clear();
                    return;
                }

                _isDragging = false;
                _draggingSet.Clear();
                return;
            }

            if (_isSquareDragging) return;

            if (evt.type != EventType.MouseDown && evt.type != EventType.MouseDrag) return;
            if (evt.type == EventType.MouseDrag && !evt.shift) _isDragging = true;
            if (evt.shift)
            {
                _isSquareDragging = true;
                _dragStartPosition = target;
                evt.Use();
                return;
            }

            if (!_isDragging || !_draggingSet.Contains(target))
            {
                action(target);
                _draggingSet.Add(target);
            }

            for (var i = -_settings.brushSize; i <= _settings.brushSize; i++)
            {
                for (var j = -_settings.brushSize; j <= _settings.brushSize; j++)
                {
                    if (i == 0 && j == 0) continue;
                    var addPos = target + new Vector3Int(i, 0, j);
                    if (_isDragging && _draggingSet.Contains(addPos)) continue;
                    action(addPos);
                    _draggingSet.Add(addPos);
                }
            }
        }

        public void RefreshPalette()
        {
            var container = new GroupBox {name = "Palette", style = {position = Position.Relative}};
            container.AddToClassList("preview-container");
            var prev = rootVisualElement.Q("Palette");
            if (prev != null) rootVisualElement.Remove(prev);
            var palette = _settings.palette;
            if (palette == null || palette.Count == 0) return;
            
            var buttons = new List<Button>();
            
            foreach (var block in palette.Blocks)
            {
                var button = new Button
                {
                    userData = block
                };
                buttons.Add(button);
                button.AddToClassList("preview");
                var texture = block.GetTexture();
                var img = new Image {image = texture};
                img.AddToClassList("preview");
                button.Add(img);
                button.Add(new Label(block.Name));
                container.Add(button);
            }

            void SelectButton(VisualElement button)
            {
                foreach (var btn in buttons) btn.RemoveFromClassList("preview-active");
                if (button?.userData is not IBlockyPiece block) return;
                if (_settings.Selected == block) _settings.Selected = null;
                else
                {
                    _settings.Selected = block;
                    button.AddToClassList("preview-active");
                }
                Repaint();
            }

            container.AddManipulator(new Clickable(() => { SelectButton(null); }));
            foreach (var btn in buttons) btn.RegisterCallback<ClickEvent>(_ => SelectButton(btn));
            rootVisualElement.Insert(1, container);
        }

        private void RemoveBlockySelection(Vector3Int pos)
        {
            if (CurrentBlocky == null) return;
            if (_map.TryGetValue(new BlockyObjectKey(pos, CurrentBlocky.Layer), out var obj))
            {
                Selection.objects = Selection.objects.Where(o => o != obj.gameObject).ToArray();
            }
        }

        private void AddBlockySelection(Vector3Int pos)
        {
            if (CurrentBlocky == null) return;
            if (_map.TryGetValue(new BlockyObjectKey(pos, CurrentBlocky.Layer), out var obj))
            {
                Selection.objects = Selection.objects.Append(obj.gameObject).ToArray();
            }
        }

        private void RemoveBlockyObject(Vector3Int pos)
        {
            if (CurrentBlocky == null) return;
            if (_map.TryGetValue(new BlockyObjectKey(pos, CurrentBlocky.Layer), out var blocky))
            {
                _map.Remove(blocky);
                DestroyImmediate(blocky.gameObject);
                SetNeighbourObjects(pos);
            }
        }

        private void AddBlockyObject(Vector3Int pos)
        {
            if (CurrentBlocky == null) return;
            var key = new BlockyObjectKey(pos, CurrentBlocky.Layer);
            if (_map.TryGetValue(new BlockyObjectKey(pos, CurrentBlocky.Layer), out var blocky))
            {
                _map.Remove(blocky);
                DestroyImmediate(blocky.gameObject);
            }

            var block = CurrentBlocky.GetPrefab(_map, key);
            if (block == null) throw new ArgumentNullException(nameof(block));
            block.transform.position = pos;
            if (CurrentBlocky is not BlockyRuleSet) block.transform.rotation = Quaternion.Euler(_settings.rotation);
            var parent = _settings.parentSetter.GetParent(block);
            if (_settings.randomRotation && block.allowRandomRotation)
            {
                var rnd = Random.Range(0, 4);
                block.transform.rotation = Quaternion.Euler(new Vector3(0, rnd * 90, 0));
            }
            block.transform.SetParent(parent);
            _map.Add(block);
            SetNeighbourObjects(pos);
        }

        private void SetNeighbourObjects(Vector3Int pos)
        {
            if (CurrentBlocky is not BlockyRuleSet ruleSet) return;
            foreach (var dir in BlockyRule.Neighbours)
            {
                if (dir == Vector3Int.zero) continue;
                var rulePos = pos + dir;
                var ruleKey = new BlockyObjectKey(rulePos, CurrentBlocky.Layer);
                if (_map.TryGetValue(ruleKey, out var existingBlock) &&
                    existingBlock.TryGetComponent<BlockyRuleBehaviour>(out var ruleBehaviour) &&
                    ruleBehaviour.ruleSet == ruleSet)
                {
                    var update = ruleSet.GetPrefab(_map, ruleKey);
                    var existingTransform = existingBlock.transform;
                    update.transform.position = existingTransform.position;
                    update.transform.SetParent(existingTransform.parent);
                    _map.Add(update);
                    DestroyImmediate(existingBlock.gameObject);
                }
            }
        }

        public void PopulateMap()
        {
            _map = new BlockyObjectMap();
            var objs = FindObjectsOfType<BlockyObject>();
            foreach (var obj in objs) _map.Add(obj);
        }
    }
}