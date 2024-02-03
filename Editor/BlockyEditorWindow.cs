﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
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
        private int _undoGroup;
        private string _parentCache;
        private VisualElement _playButton;
        private VisualElement _settingsView;

        [MenuItem("Tools/Blocky/Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<BlockyEditorWindow>();
            window.titleContent = new GUIContent("BlockyWorld");
            window.Show();
        }

        private void OnEnable()
        {
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorApplication.playModeStateChanged += OnPlaymodeChange;
            if (_settings == null) _settings = BlockyEditorSettings.GetOrCreateSettings();
            if (_settings.useUndo) Undo.undoRedoPerformed += PopulateMap;

            _placementShader = Shader.Find("Blocky/Placement");
            PopulateMap();
            if (_settings.parentSetter == null)
            {
                _settings.parentSetter =
                    AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_settings))
                        .First(a => a is BlockyParentSetter) as BlockyParentSetter;
            }

            _settings.editMode = BlockyEditMode.None;
            _serializedSettings ??= new SerializedObject(_settings);
            _serializedSettings.ApplyModifiedProperties();
            var stylesheet = Resources.Load<StyleSheet>("BlockyEditor");
            rootVisualElement.styleSheets.Add(stylesheet);
            _settingsView ??= new BlockySettingsElement(this, _serializedSettings);
            rootVisualElement.Add(_settingsView);
            RefreshPalette();
            Repaint();
            
            var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            var currentToolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
            var guiViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");
            var iWindowBackendType =
                typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IWindowBackend");
            var guiBackend = guiViewType.GetProperty("windowBackend",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var viewVisualTree = iWindowBackendType.GetProperty("visualTree",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var windowBackend = guiBackend.GetValue(currentToolbar);
            var toolbarElement = (VisualElement) viewVisualTree.GetValue(windowBackend, null);
            _playButton = toolbarElement.Q("Play");
        }

        private void OnPlaymodeChange(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredEditMode:
                    RefreshPalette();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
            }
        }


        private void OnDisable()
        {
            if (_settings.useUndo) Undo.undoRedoPerformed -= PopulateMap;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            EditorApplication.playModeStateChanged -= OnPlaymodeChange;
        }

        private void OnSceneClosing(Scene scene, bool removingScene)
        {
            if (_placementObject != null) DestroyImmediate(_placementObject);
            if (_settings.editMode == BlockyEditMode.None) return;

            // In Painting/Selection mode while scene is closing, process the scenes beforehand
            var preprocessors = GetScenePreprocessors();
            foreach (var preprocessor in preprocessors) preprocessor.ProcessScene(this, scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public void OnBlockyModeChange(BlockyEditMode mode)
        {
            var preprocessors = GetScenePreprocessors();
            var openSceneCount = SceneManager.sceneCount;

            
            if (mode == BlockyEditMode.None)
            {
                _playButton.SetEnabled(true);
                _playButton.style.borderBottomColor = new StyleColor(Color.clear);
                _playButton.style.borderTopColor = new StyleColor(Color.clear);
                _playButton.tooltip = "Play";
                for (var i = 0; i < openSceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    foreach (var preprocessor in preprocessors)
                        preprocessor.ProcessScene(this, scene);
                }

                EditorSceneManager.MarkAllScenesDirty();
                EditorSceneManager.SaveOpenScenes();
            }
            else
            {
                _playButton.style.borderTopColor = new StyleColor(Color.red * 0.8f);
                _playButton.style.borderBottomColor = new StyleColor(Color.red * 0.8f);
                _playButton.tooltip = $"Exit Blocky {_settings.editMode} Mode before entering PlayMode";
                _playButton.SetEnabled(false);
                for (var i = 0; i < openSceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    foreach (var preprocessor in preprocessors)
                        preprocessor.RevertScene(this, scene);
                }

                EditorSceneManager.MarkAllScenesDirty();
            }
        }

        private static List<IBlockyScenePreprocessor> GetScenePreprocessors()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var results = new List<IBlockyScenePreprocessor>();
            foreach (var assembly in assemblies)
            {
                var preprocessors = assembly.GetTypes()
                    .Where(t =>
                        !t.IsAbstract &&
                        t.GetInterfaces().Contains(typeof(IBlockyScenePreprocessor)))
                    .Select(t => (IBlockyScenePreprocessor) Activator.CreateInstance(t)).ToList();
                results.AddRange(preprocessors);
            }

            results.Sort((a, b) => b.Order - a.Order);
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
            if (!BlockyEditorUtilities.TryGetTargetPoint(Event.current.mousePosition,
                    _settings.gridHeight,
                    out _settings.target)) return;
            BlockyEditorUtilities.SetTargetVisualization(_settings.target, _settings.editMode,
                _settings.brushSize,
                _isSquareDragging, _draggingSquareList, _dragStartPosition);
            BlockyEditorUtilities.SetPlacementVisualization(_settings.target, _settings.rotation,
                _settings.editMode,
                CurrentBlocky, _placementShader, ref _placementObject);
            _settings.parentSetter.SetVisualization(_settings.target, _settings.gridHeight);

            HandleInput(_settings.target);
            HandleUtility.Repaint();
        }

        private bool _isControllingCamera;

        private void HandleInput(Vector3Int target)
        {
            var evt = Event.current;
            _isControllingCamera = evt.type switch
            {
                EventType.MouseDown when evt.button == 1 => true,
                EventType.MouseUp when evt.button == 1 => false,
                _ => _isControllingCamera
            };

            if (_isControllingCamera) return;
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
                        _settings.rotation.y =
                            BlockyUtilities.ClampAngle(_settings.rotation.y + 90);
                        break;
                    case KeyCode.E:
                        _settings.rotation.y =
                            BlockyUtilities.ClampAngle(_settings.rotation.y - 90);
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

                if (!evt.control && !evt.shift) evt.Use();
                Repaint();
                return;
            }

            if (evt.type == EventType.Layout)
            {
                var controlId = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);
                HandleUtility.AddDefaultControl(controlId);
            }

            Action<Vector3Int, int> action = _settings.editMode switch
            {
                BlockyEditMode.None => null,
                BlockyEditMode.Paint => evt.control ? RemoveBlockyObject : AddBlockyObject,
                BlockyEditMode.Select => evt.control ? RemoveBlockySelection : AddBlockySelection,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (action == null || evt.button != decimal.Zero) return;

            Undo.IncrementCurrentGroup();
            _undoGroup = _draggingSet.Count == 0 ? Undo.GetCurrentGroup() : _undoGroup;
            if (evt.type == EventType.MouseUp)
            {
                if (CurrentBlocky == null)
                {
                    Debug.LogWarning("No BlockyObject Selected from Palette.");
                    return;
                }

                evt.Use();
                if (_isSquareDragging)
                {
                    _isSquareDragging = false;
                    _dragStartPosition = Vector3Int.zero;
                    foreach (var pos in _draggingSquareList) action(pos, _undoGroup);
                    _draggingSquareList.Clear();
                    return;
                }

                _isDragging = false;
                _draggingSet.Clear();
                return;
            }

            if (_isSquareDragging) return;

            if (evt.type != EventType.MouseDown && evt.type != EventType.MouseDrag) return;
            if (evt.type == EventType.MouseDrag && !evt.shift)
            {
                if (CurrentBlocky == null)
                {
                    Debug.LogWarning("No BlockyObject Selected from Palette.");
                    return;
                }

                _isDragging = true;
            }

            if (evt.shift)
            {
                if (CurrentBlocky == null)
                {
                    Debug.LogWarning("No BlockyObject Selected from Palette.");
                    return;
                }

                _isSquareDragging = true;
                _dragStartPosition = target;
                evt.Use();
                return;
            }

            if (!_isDragging || !_draggingSet.Contains(target))
            {
                if (CurrentBlocky == null)
                {
                    Debug.LogWarning("No BlockyObject Selected from Palette.");
                    return;
                }

                action(target, _undoGroup);
                _draggingSet.Add(target);
            }

            for (var i = -_settings.brushSize; i <= _settings.brushSize; i++)
            {
                for (var j = -_settings.brushSize; j <= _settings.brushSize; j++)
                {
                    if (i == 0 && j == 0) continue;
                    var addPos = target + new Vector3Int(i, 0, j);
                    if (_isDragging && _draggingSet.Contains(addPos)) continue;
                    action(addPos, _undoGroup);
                    _draggingSet.Add(addPos);
                }
            }
        }

        public void RefreshPalette()
        {
            var scroll = new ScrollView
            {
                name = "Palette",
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible
            };
            scroll.AddToClassList("preview-scroll");
            var prev = rootVisualElement.Q("Palette");
            if (prev != null) rootVisualElement.Remove(prev);

            var container = new GroupBox {style = {position = Position.Relative}};
            container.AddToClassList("preview-container");
            var palette = _settings.palette;
            if (palette == null || palette.Count == 0) return;
            var paletteInspector = new InspectorElement(_settings.palette);
            scroll.Add(paletteInspector);

            var layerLabel = new Label {name = "LayerLabel"};
            layerLabel.AddToClassList("layer-label");
            scroll.Add(layerLabel);
            scroll.Add(container);
            rootVisualElement.Insert(1, scroll);
            EditorCoroutineUtility.StartCoroutine(CreatePreviewIcons(palette, container), this);
        }

        private IEnumerator CreatePreviewIcons(BlockyPalette palette, VisualElement container)
        {
            var buttons = new List<Button>();
            foreach (var block in palette.Blocks)
            {
                var button = new Button
                {
                    userData = block
                };
                buttons.Add(button);
                button.AddToClassList("preview");
                Texture2D texture = null;
                var count = 0;
                // in case GetTexture isn't implemented we'll add a failsafe count
                while (texture == null && count < 20)
                {
                    texture = block.GetTexture();
                    count++;
                    yield return null;
                }

                var img = new Image {image = texture};
                img.AddToClassList("preview");
                button.Add(img);
                button.Add(new Label(block.Name));
                container.Add(button);
            }

            container.AddManipulator(new Clickable(() => { SelectButton(null, null); }));
            foreach (var btn in buttons)
                btn.RegisterCallback<ClickEvent>(evt => SelectButton(evt, btn));
            yield break;

            void SelectButton(ClickEvent evt, VisualElement button)
            {
                foreach (var btn in buttons) btn.RemoveFromClassList("preview-active");
                if (button?.userData is not IBlockyPiece block) return;
                if (_settings.Selected == block) _settings.Selected = null;
                else
                {
                    _settings.Selected = block;
                    var layerLabel = container.parent.Q<Label>("LayerLabel");
                    layerLabel.text = $"Layer: {block.Layer.name}";
                    button.AddToClassList("preview-active");
                    if (evt.ctrlKey)
                    {
                        Selection.activeObject = block switch
                        {
                            BlockyObject obj => obj,
                            BlockyRuleSet rule => rule,
                            BlockyRandomizer rnd => rnd,
                            _ => Selection.activeObject
                        };
                    }
                }

                Repaint();
            }
        }

        private void RemoveBlockySelection(Vector3Int pos, int undoGroup)
        {
            if (CurrentBlocky == null) return;
            if (_map.TryGetValue(new BlockyObjectKey(pos, CurrentBlocky.Layer), out var obj))
            {
                Selection.objects = Selection.objects.Where(o => o != obj.gameObject).ToArray();
            }
        }

        private void AddBlockySelection(Vector3Int pos, int undoGroup)
        {
            if (CurrentBlocky == null) return;
            if (_map.TryGetValue(new BlockyObjectKey(pos, CurrentBlocky.Layer), out var obj))
            {
                Selection.objects = Selection.objects.Append(obj.gameObject).ToArray();
            }
        }

        private void RemoveBlockyObject(Vector3Int pos, int undoGroup)
        {
            if (CurrentBlocky == null) return;
            if (_map.TryGetValue(new BlockyObjectKey(pos, CurrentBlocky.Layer), out var blocky))
            {
                _map.Remove(blocky);
                if (_settings.useUndo) Undo.DestroyObjectImmediate(blocky.gameObject);
                else DestroyImmediate(blocky.gameObject);

                SetNeighbourObjects(pos);
                if (_settings.useUndo)
                {
                    Undo.SetCurrentGroupName("Remove Blocky Objects");
                    Undo.CollapseUndoOperations(undoGroup);
                    Undo.ClearUndo(
                        _placementObject); // shouldn't be needed but having weird effects on the placement object
                }
            }
        }

        private void AddBlockyObject(Vector3Int pos, int undoGroup)
        {
            if (CurrentBlocky == null) return;
            var key = new BlockyObjectKey(pos, CurrentBlocky.Layer);

            if (_map.TryGetValue(new BlockyObjectKey(pos, CurrentBlocky.Layer), out var blocky))
            {
                _map.Remove(blocky);
                if (_settings.useUndo) Undo.DestroyObjectImmediate(blocky.gameObject);
                else DestroyImmediate(blocky.gameObject);
            }

            var block = CurrentBlocky.GetPrefab(_map, key);
            if (_settings.useUndo)
            {
                Undo.RegisterCreatedObjectUndo(block.gameObject, "Create new Blocky Object");
                Undo.RecordObject(block.transform, "Set Blocky Object Transform");
            }

            if (block == null) throw new ArgumentNullException(nameof(block));
            block.transform.position = pos;
            if (CurrentBlocky is not BlockyRuleSet)
                block.transform.rotation = Quaternion.Euler(_settings.rotation);
            var parent = _settings.parentSetter.GetParent(block);
            if (_settings.randomRotation && block.allowRandomRotation)
            {
                var rnd = Random.Range(0, 4);
                block.transform.rotation = Quaternion.Euler(new Vector3(0, rnd * 90, 0));
            }

            if (_settings.useUndo)
                Undo.SetTransformParent(block.transform, parent, "Set Blocky Object Parent");
            else block.transform.SetParent(parent);

            _map.Add(block, _settings.useUndo);
            SetNeighbourObjects(pos);
            if (_settings.useUndo)
            {
                Undo.SetCurrentGroupName("Create new Blocky Objects");
                Undo.CollapseUndoOperations(undoGroup);
                Undo.ClearUndo(
                    _placementObject); // shouldn't be needed but having weird effects on the placement object
            }
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
                    if (_settings.useUndo)
                    {
                        Undo.RegisterCreatedObjectUndo(update.gameObject,
                            "Create neighbouring Blocky Object");
                        Undo.RecordObject(update.transform,
                            "Set Neighbouring Blocky Object Transform");
                    }

                    var existingTransform = existingBlock.transform;
                    update.transform.position = existingTransform.position;
                    if (_settings.useUndo)
                        Undo.SetTransformParent(update.transform, existingTransform.parent,
                            "Set neighbour Parent");
                    else update.transform.SetParent(existingTransform.parent);
                    _map.Add(update, _settings.useUndo);
                }
            }
        }

        public void PopulateMap()
        {
            _map = new BlockyObjectMap();
            var objs = FindObjectsOfType<BlockyObject>();
            foreach (var obj in objs) _map.Add(obj, _settings.useUndo);
        }
    }
}