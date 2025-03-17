using System;
using UnityEditor;
using UnityEngine;

namespace PeartreeGames.Blocky.World.Editor
{
    [Serializable]
    public class SceneObject
    {
        public GameObject gameObject;
        [HideInInspector]
        public string id;
    }
}