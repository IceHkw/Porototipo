// ====================================
// PoolCategoryDrawer.cs
// Custom Property Drawer para categorías de pools
// ====================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(PoolCategory))]
public class PoolCategoryDrawer : PropertyDrawer
{
    private Dictionary<string, ReorderableList> reorderableLists = new Dictionary<string, ReorderableList>();
    private const float HEADER_HEIGHT = 22f;
    private const float ELEMENT_HEIGHT = 80f;
    private const float SPACING = 5f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty categoryNameProp = property.FindPropertyRelative("categoryName");
        SerializedProperty isExpandedProp = property.FindPropertyRelative("isExpanded");
        SerializedProperty poolsProp = property.FindPropertyRelative("pools");

        // Header con foldout estilizado
        Rect headerRect = new Rect(position.x, position.y, position.width, HEADER_HEIGHT);
        DrawCategoryHeader(headerRect, categoryNameProp, isExpandedProp, poolsProp);

        // Si está expandido, mostrar la lista
        if (isExpandedProp.boolValue)
        {
            Rect listRect = new Rect(position.x + 10, position.y + HEADER_HEIGHT + SPACING,
                                     position.width - 10, position.height - HEADER_HEIGHT - SPACING);
            DrawPoolsList(listRect, property, poolsProp);
        }

        EditorGUI.EndProperty();
    }

    private void DrawCategoryHeader(Rect rect, SerializedProperty nameProp, SerializedProperty expandedProp, SerializedProperty poolsProp)
    {
        // Background
        Color bgColor = expandedProp.boolValue ? new Color(0.2f, 0.2f, 0.2f, 0.3f) : new Color(0.15f, 0.15f, 0.15f, 0.3f);
        EditorGUI.DrawRect(rect, bgColor);

        // Foldout
        Rect foldoutRect = new Rect(rect.x + 5, rect.y + 2, 20, rect.height - 4);
        expandedProp.boolValue = EditorGUI.Foldout(foldoutRect, expandedProp.boolValue, GUIContent.none, true);

        // Category name
        Rect nameRect = new Rect(rect.x + 25, rect.y + 2, rect.width - 100, rect.height - 4);
        EditorGUI.BeginChangeCheck();
        string newName = EditorGUI.TextField(nameRect, nameProp.stringValue, EditorStyles.boldLabel);
        if (EditorGUI.EndChangeCheck())
        {
            nameProp.stringValue = newName;
        }

        // Pool count
        Rect countRect = new Rect(rect.x + rect.width - 70, rect.y + 2, 65, rect.height - 4);
        EditorGUI.LabelField(countRect, $"({poolsProp.arraySize} pools)", EditorStyles.miniLabel);
    }

    private void DrawPoolsList(Rect rect, SerializedProperty property, SerializedProperty poolsProp)
    {
        string key = property.propertyPath;

        if (!reorderableLists.ContainsKey(key))
        {
            ReorderableList list = new ReorderableList(poolsProp.serializedObject, poolsProp, true, true, true, true);

            list.drawHeaderCallback = (Rect headerRect) =>
            {
                EditorGUI.LabelField(headerRect, "Pool Items");
            };

            list.elementHeight = ELEMENT_HEIGHT;

            list.drawElementCallback = (Rect elementRect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = poolsProp.GetArrayElementAtIndex(index);
                DrawPoolItem(elementRect, element, index);
            };

            list.onAddCallback = (ReorderableList l) =>
            {
                poolsProp.InsertArrayElementAtIndex(poolsProp.arraySize);
                SerializedProperty newElement = poolsProp.GetArrayElementAtIndex(poolsProp.arraySize - 1);

                // Resetear valores
                newElement.FindPropertyRelative("poolID").stringValue = "";
                newElement.FindPropertyRelative("prefab").objectReferenceValue = null;
                newElement.FindPropertyRelative("initialSize").intValue = 10;
                newElement.FindPropertyRelative("allowExpansion").boolValue = true;
                newElement.FindPropertyRelative("expansionAmount").intValue = 5;
            };

            reorderableLists[key] = list;
        }

        reorderableLists[key].DoList(rect);
    }

    private void DrawPoolItem(Rect rect, SerializedProperty element, int index)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float padding = 2f;
        float y = rect.y + padding;

        // Background alternado
        if (index % 2 == 0)
        {
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.1f));
        }

        // Pool ID y Prefab en la misma línea
        float halfWidth = (rect.width - padding) / 2f;

        SerializedProperty poolIDProp = element.FindPropertyRelative("poolID");
        SerializedProperty prefabProp = element.FindPropertyRelative("prefab");

        Rect idRect = new Rect(rect.x + padding, y, halfWidth - padding, lineHeight);
        poolIDProp.stringValue = EditorGUI.TextField(idRect, "ID", poolIDProp.stringValue);

        Rect prefabRect = new Rect(rect.x + halfWidth + padding, y, halfWidth - padding, lineHeight);
        prefabProp.objectReferenceValue = EditorGUI.ObjectField(prefabRect, "Prefab",
            prefabProp.objectReferenceValue, typeof(GameObject), false);

        y += lineHeight + padding;

        // Initial Size y Expansion en la misma línea
        SerializedProperty initialSizeProp = element.FindPropertyRelative("initialSize");
        SerializedProperty allowExpansionProp = element.FindPropertyRelative("allowExpansion");
        SerializedProperty expansionAmountProp = element.FindPropertyRelative("expansionAmount");

        Rect sizeRect = new Rect(rect.x + padding, y, halfWidth - padding, lineHeight);
        initialSizeProp.intValue = EditorGUI.IntField(sizeRect, "Initial Size", initialSizeProp.intValue);

        y += lineHeight + padding;

        // Allow Expansion checkbox
        Rect expansionRect = new Rect(rect.x + padding, y, 120, lineHeight);
        allowExpansionProp.boolValue = EditorGUI.Toggle(expansionRect, "Allow Expansion", allowExpansionProp.boolValue);

        // Expansion Amount (solo si expansion está habilitada)
        if (allowExpansionProp.boolValue)
        {
            Rect amountRect = new Rect(rect.x + 140, y, rect.width - 145, lineHeight);
            expansionAmountProp.intValue = EditorGUI.IntField(amountRect, "Amount", expansionAmountProp.intValue);
        }

        // Validación visual
        if (string.IsNullOrEmpty(poolIDProp.stringValue) || prefabProp.objectReferenceValue == null)
        {
            Rect warningRect = new Rect(rect.x + rect.width - 20, rect.y + padding, 16, 16);
            GUI.color = Color.yellow;
            GUI.Label(warningRect, "⚠", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty isExpandedProp = property.FindPropertyRelative("isExpanded");
        SerializedProperty poolsProp = property.FindPropertyRelative("pools");

        float height = HEADER_HEIGHT;

        if (isExpandedProp.boolValue)
        {
            height += SPACING;

            // Altura de la ReorderableList
            float listHeight = 35f; // Header
            listHeight += (ELEMENT_HEIGHT + 2) * Mathf.Max(1, poolsProp.arraySize); // Elements
            listHeight += 25f; // Footer

            height += listHeight;
        }

        return height;
    }
}

// Custom Editor para ObjectPoolManager
[CustomEditor(typeof(ObjectPoolManager))]
public class ObjectPoolManagerEditor : Editor
{
    private ObjectPoolManager poolManager;
    private SerializedProperty poolCategoriesProp;

    private void OnEnable()
    {
        poolManager = (ObjectPoolManager)target;
        poolCategoriesProp = serializedObject.FindProperty("poolCategories");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Header estilizado
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("OBJECT POOL MANAGER", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Mostrar propiedades antes de las categorías
        DrawPropertiesExcluding(serializedObject, "poolCategories");

        EditorGUILayout.Space(10);

        // Categorías con estilo mejorado
        EditorGUILayout.LabelField("Pool Categories", EditorStyles.boldLabel);

        // Botones de control
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Category", GUILayout.Width(100)))
        {
            poolCategoriesProp.InsertArrayElementAtIndex(poolCategoriesProp.arraySize);
            SerializedProperty newCategory = poolCategoriesProp.GetArrayElementAtIndex(poolCategoriesProp.arraySize - 1);
            newCategory.FindPropertyRelative("categoryName").stringValue = $"Category {poolCategoriesProp.arraySize}";
            newCategory.FindPropertyRelative("isExpanded").boolValue = true;
            newCategory.FindPropertyRelative("pools").ClearArray();
        }

        if (GUILayout.Button("Collapse All", GUILayout.Width(100)))
        {
            for (int i = 0; i < poolCategoriesProp.arraySize; i++)
            {
                poolCategoriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("isExpanded").boolValue = false;
            }
        }

        if (GUILayout.Button("Expand All", GUILayout.Width(100)))
        {
            for (int i = 0; i < poolCategoriesProp.arraySize; i++)
            {
                poolCategoriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("isExpanded").boolValue = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Dibujar categorías
        for (int i = 0; i < poolCategoriesProp.arraySize; i++)
        {
            SerializedProperty category = poolCategoriesProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(category);

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Eliminar Categoría",
                    $"¿Estás seguro de eliminar la categoría '{category.FindPropertyRelative("categoryName").stringValue}'?",
                    "Sí", "No"))
                {
                    poolCategoriesProp.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(2);
        }

        // Runtime controls
        if (Application.isPlaying && poolManager.IsInitialized)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Log Pool Statistics"))
            {
                poolManager.LogStats();
            }

            if (GUILayout.Button("Return All Objects"))
            {
                poolManager.TestReturnAll();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif