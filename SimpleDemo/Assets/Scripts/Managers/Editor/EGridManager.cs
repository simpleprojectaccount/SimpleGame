using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Vertigo.Managers
{
    [CustomEditor(typeof(GridManager))]
    public class EGridManager : Editor
    {
        private GridManager _gm;
        private int _colorCount;
        public override void OnInspectorGUI()
        {
            if (!_gm) return;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical("Box");
            _gm.gridInfo = EditorGUILayout.Vector2IntField("Grid sizes:", _gm.gridInfo);
            EditorGUILayout.EndVertical();


            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical("Box");

            _colorCount = EditorGUILayout.DelayedIntField("Number of colors", _colorCount);
            if (EditorGUI.EndChangeCheck())
                HandleColorNumberInput();
            DrawColorPickers();
            EditorGUILayout.EndVertical();


            WarningsField();
        }

        protected void OnEnable()
        {
            _gm = target as GridManager;
            _colorCount = _gm.colors.Count;
        }

        private void DrawColorPickers()
        {
            EditorGUILayout.BeginVertical("Box");
            for (int i = 0; i < _gm.colors.Count; i++)
                _gm.colors[i] = EditorGUILayout.ColorField(_gm.colors[i]);
            EditorGUILayout.EndVertical();
        }

        private void HandleColorNumberInput()
        {
            if (_colorCount <= 0) _colorCount = 0;
            int deltaNumberOfColors = _colorCount - _gm.colors.Count;
            if (deltaNumberOfColors > 0)
            {
                for (int i = 0; i < deltaNumberOfColors; i++)
                    if (_gm.colors.Count == 0)
                        _gm.colors.Add(Color.white);
                    else
                        _gm.colors.Add(_gm.colors[_gm.colors.Count - 1]);
            }
            else
            {
                for (int i = 0; i < -deltaNumberOfColors; i++)
                    if (_gm.colors.Count > 0)
                        _gm.colors.RemoveAt(_gm.colors.Count - 1);
            }

        }

        private void EnsureColors()
        {
            if (_gm.colors == null)
                _gm.colors = new List<Color>();
            if (_gm.colors.Count != 0)
                _gm.colors.Clear();

            _gm.colors.Add(new Color32(227, 74, 59, 255));  // pastel red 
            _gm.colors.Add(new Color32(81, 148, 209, 255)); // pastel blue
            _gm.colors.Add(new Color32(234, 191, 27, 255)); // pastel yellow
            _gm.colors.Add(new Color32(68, 185, 109, 255)); // pastel green
            _gm.colors.Add(new Color32(148, 88, 164, 255)); // pastel purple
            _colorCount = _gm.colors.Count;
        }


        private void WarningsField()
        {
            var c = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1, 0, 0, 0.5f);
            if (_gm.gridInfo.x < 2 || _gm.gridInfo.y < 2)
            {
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("Grid height and width cannot be smaller than 2");
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("Fix known errors"))
                    FixKnownErrors();
            }
            if (_colorCount <= 2)
            {
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("At least 3 color has to be choosen.");
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("Fix known errors"))
                    FixKnownErrors();
            }

            GUI.backgroundColor = c;

        }

        // Fix faulty settings by reseting them to default values
        private void FixKnownErrors()
        {
            if (_gm.gridInfo.x < 2)
                _gm.gridInfo.x = 8;
            if (_gm.gridInfo.y < 2)
                _gm.gridInfo.y = 9;
            if (_colorCount < 4)
                EnsureColors();
        }
    }
}