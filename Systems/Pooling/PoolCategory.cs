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
    private const float SPACING = 5f;

    // --- CAMBIO CLAVE: Ya no usamos una altura fija para los elementos ---
    // private const float ELEMENT_HEIGHT = 125f; 

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty categoryNameProp = property.FindPropertyRelative("categoryName");
        SerializedProperty isExpandedProp = property.FindPropertyRelative("isExpanded");
        SerializedProperty poolsProp = property.FindPropertyRelative("pools");

        Rect headerRect = new Rect(position.x, position.y, position.width, HEADER_HEIGHT);
        DrawCategoryHeader(headerRect, categoryNameProp, isExpandedProp, poolsProp);

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
        Color bgColor = expandedProp.boolValue ? new Color(0.2f, 0.2f, 0.2f, 0.3f) : new Color(0.15f, 0.15f, 0.15f, 0.3f);
        EditorGUI.DrawRect(rect, bgColor);

        Rect foldoutRect = new Rect(rect.x + 5, rect.y + 2, 20, rect.height - 4);
        expandedProp.boolValue = EditorGUI.Foldout(foldoutRect, expandedProp.boolValue, GUIContent.none, true);

        Rect nameRect = new Rect(rect.x + 25, rect.y + 2, rect.width - 100, rect.height - 4);
        EditorGUI.BeginChangeCheck();
        string newName = EditorGUI.TextField(nameRect, nameProp.stringValue, EditorStyles.boldLabel);
        if (EditorGUI.EndChangeCheck())
        {
            nameProp.stringValue = newName;
        }

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

            // --- CAMBIO CLAVE: Usamos un callback para que la altura de cada elemento sea dinámica ---
            list.elementHeightCallback = (index) =>
            {
                return GetElementHeight(poolsProp.GetArrayElementAtIndex(index));
            };

            list.drawElementCallback = (Rect elementRect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = poolsProp.GetArrayElementAtIndex(index);
                // Ajustamos el rectángulo para tener un pequeño margen
                elementRect.y += 2;
                elementRect.height -= 4;
                DrawPoolItem(elementRect, element, index);
            };

            list.onAddCallback = (ReorderableList l) =>
            {
                poolsProp.InsertArrayElementAtIndex(poolsProp.arraySize);
                SerializedProperty newElement = poolsProp.GetArrayElementAtIndex(poolsProp.arraySize - 1);

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
        // --- CAMBIO CLAVE: Aumentamos el padding y usamos la altura real de cada propiedad ---
        float verticalPadding = 4f;
        float y = rect.y;

        if (index % 2 == 0)
        {
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.1f));
        }

        SerializedProperty poolIDProp = element.FindPropertyRelative("poolID");
        SerializedProperty prefabProp = element.FindPropertyRelative("prefab");
        SerializedProperty initialSizeProp = element.FindPropertyRelative("initialSize");
        SerializedProperty allowExpansionProp = element.FindPropertyRelative("allowExpansion");
        SerializedProperty expansionAmountProp = element.FindPropertyRelative("expansionAmount");

        Rect currentRect = new Rect(rect.x, y, rect.width, 0);

        // 1. Pool ID
        currentRect.height = EditorGUI.GetPropertyHeight(poolIDProp);
        EditorGUI.PropertyField(currentRect, poolIDProp);
        y += currentRect.height + verticalPadding;
        currentRect.y = y;

        // 2. Prefab
        currentRect.height = EditorGUI.GetPropertyHeight(prefabProp);
        EditorGUI.PropertyField(currentRect, prefabProp);
        y += currentRect.height + verticalPadding;
        currentRect.y = y;

        // 3. Initial Size
        currentRect.height = EditorGUI.GetPropertyHeight(initialSizeProp);
        EditorGUI.PropertyField(currentRect, initialSizeProp);
        y += currentRect.height + verticalPadding;
        currentRect.y = y;

        // 4. Allow Expansion
        currentRect.height = EditorGUI.GetPropertyHeight(allowExpansionProp);
        EditorGUI.PropertyField(currentRect, allowExpansionProp);
        y += currentRect.height + verticalPadding;
        currentRect.y = y;

        // 5. Expansion Amount
        if (allowExpansionProp.boolValue)
        {
            currentRect.height = EditorGUI.GetPropertyHeight(expansionAmountProp);
            EditorGUI.PropertyField(currentRect, expansionAmountProp);
        }

        if (string.IsNullOrEmpty(poolIDProp.stringValue) || prefabProp.objectReferenceValue == null)
        {
            Rect warningRect = new Rect(rect.x + rect.width - 20, rect.y + 2, 16, 16);
            GUI.color = Color.yellow;
            GUI.Label(warningRect, "⚠", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }
    }

    // --- CAMBIO CLAVE: Nuevo método para calcular la altura de un solo elemento de la lista ---
    private float GetElementHeight(SerializedProperty element)
    {
        float verticalPadding = 4f;
        float totalHeight = 0f;

        totalHeight += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("poolID")) + verticalPadding;
        totalHeight += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("prefab")) + verticalPadding;
        totalHeight += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("initialSize")) + verticalPadding;
        totalHeight += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("allowExpansion")) + verticalPadding;

        if (element.FindPropertyRelative("allowExpansion").boolValue)
        {
            totalHeight += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("expansionAmount")) + verticalPadding;
        }

        return totalHeight + verticalPadding; // Un poco de espacio extra al final
    }

    // --- CAMBIO CLAVE: La altura total ahora suma las alturas dinámicas de cada elemento ---
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty isExpandedProp = property.FindPropertyRelative("isExpanded");
        if (!isExpandedProp.boolValue)
        {
            return HEADER_HEIGHT;
        }

        float height = HEADER_HEIGHT + SPACING;

        float listHeight = 40f; // Altura del header y footer de la lista
        SerializedProperty poolsProp = property.FindPropertyRelative("pools");

        if (poolsProp.arraySize > 0)
        {
            for (int i = 0; i < poolsProp.arraySize; i++)
            {
                listHeight += GetElementHeight(poolsProp.GetArrayElementAtIndex(i)) + 5f; // +5f for list internal spacing
            }
        }
        else
        {
            listHeight += 40f; // Altura para el mensaje de lista vacía
        }

        return height + listHeight;
    }
}

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

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("OBJECT POOL MANAGER", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        DrawPropertiesExcluding(serializedObject, "poolCategories");

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Pool Categories", EditorStyles.boldLabel);

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