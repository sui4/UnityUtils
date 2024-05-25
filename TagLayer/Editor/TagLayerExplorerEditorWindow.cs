using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class TagLayerExplorerEditorWindow : EditorWindow
{
    private List<GameObject> _foundObjects = new();
    // ui elements
    private EnumField _searchTypeField;
    private TagField _tagField;
    private LayerField _layerField;
    private VisualElement _inputElement;
    private VisualElement _foundObjectsView;

    [MenuItem("Tools/Tag&LayerExplorer")]
    public static void Init()
    {
        var window = GetWindow<TagLayerExplorerEditorWindow>();
        window.titleContent = new GUIContent("Tag & Layer Explorer");
        window.Show();
    }

    private enum SearchType
    {
        Tag,
        Layer,
    }

    public void CreateGUI()
    {
        _foundObjects.Clear();

        _inputElement = new VisualElement();

        _searchTypeField = new EnumField("Search Type", SearchType.Tag);
        _searchTypeField.RegisterValueChangedCallback(OnSearchTypeChange);
        _inputElement.Add(_searchTypeField);
        _layerField = new LayerField("Layer", 0);
        _layerField.RegisterValueChangedCallback(layer => { SearchLayer(layer.newValue, _foundObjects);
            RenewSearchView(layer.newValue.ToString());
        });
        _tagField = new TagField("Tag");
        _tagField.RegisterValueChangedCallback(tag => { SearchTag(tag.newValue, _foundObjects);
            RenewSearchView(tag.newValue);
        });
        _inputElement.Add(_tagField);

        rootVisualElement.Add(_inputElement);

        _foundObjectsView = new VisualElement();
        rootVisualElement.Add(_foundObjectsView);
        Button selectAllBtn = new Button();
        selectAllBtn.text = "Select found objects";
        selectAllBtn.clicked += SelectFoundObjects;
        rootVisualElement.Add(selectAllBtn);
    }

    private void RenewSearchView(string searchTarget)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Search {_searchTypeField.text}: {searchTarget}");
        sb.AppendLine($"Found {_foundObjects.Count} objects");
        _foundObjectsView.Clear();
        VisualElement foundObjectsList = new VisualElement();
        _foundObjectsView.Add(foundObjectsList);
        foreach (GameObject obj in _foundObjects)
        {
            ObjectField field = new ObjectField();
            field.value = obj;
            field.SetEnabled(false);
            foundObjectsList.Add(field);

            sb.AppendLine(obj.name);
        }
        Debug.Log(sb.ToString());
    }

    #region callback
    private void OnSearchTypeChange(ChangeEvent<Enum> evt)
    {
        switch (evt.newValue)
        {
            case SearchType.Tag:
                _inputElement.Add(_tagField);
                if (_inputElement.Contains(_layerField))
                {
                    _inputElement.Remove(_layerField);
                }
                SearchTag(_tagField.value, _foundObjects);
                RenewSearchView(_tagField.value);

                break;
            case SearchType.Layer:
                _inputElement.Add(_layerField);
                if (_inputElement.Contains(_tagField))
                {
                    _inputElement.Remove(_tagField);
                }
                SearchLayer(_layerField.value, _foundObjects);
                RenewSearchView(_layerField.value.ToString());
                break;
        }
    }

    private void SelectFoundObjects()
    {
        if(_foundObjects.Count > 0)
        {
            Selection.objects = _foundObjects.ToArray();
        }
    }
    #endregion

    #region Search
    private static void SearchTag(string tag, List<GameObject> foundObjects)
    {
        foundObjects.Clear();
        HashSet<string> tags = new HashSet<string>(UnityEditorInternal.InternalEditorUtility.tags);
        if (!tags.Contains(tag)) return;

        GameObject[] objectsWithTargetTag = GameObject.FindGameObjectsWithTag(tag);
        foundObjects.AddRange(objectsWithTargetTag);

    }

    private static void SearchLayer(int layerId, List<GameObject> foundObjects)
    {
        foundObjects.Clear();
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (((1 << layerId) & (1 << obj.layer)) != 0)
            {
                foundObjects.Add(obj);
            }
        }
    }

    private void SearchLayer(string layerName, List<GameObject> foundObjects)
    {
        int layerId = LayerMask.NameToLayer(layerName);
        SearchLayer(layerId, foundObjects);
    }
    #endregion
}
