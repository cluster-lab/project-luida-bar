using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = System.Object;

namespace ClusterVR.CreatorKit.Editor.Goods
{
    public class GoodsListWindow : EditorWindow
    {
        static TextField searchField;

        [MenuItem("Cluster/Window/GoodsList")]
        public static void ShowWindow()
        {
            var window = GetWindow<GoodsListWindow>();
            window.titleContent = new GUIContent("GoodsList");
        }

        void OnEnable()
        {
            var root = rootVisualElement;
            root.Add(GenerateSearchField());
            root.Add(GenerateGoodsList());

            searchField.RegisterValueChangedCallback(_ =>
            {
                root.RemoveAt(1);
                root.Add(GenerateGoodsList());
            });
        }

        static List<Exhibit.Goods.Implements.Goods> GetGoodsFromHierarchy()
        {
            var currentScene = SceneManager.GetActiveScene();
            var rootObjects = currentScene.GetRootGameObjects();

            return rootObjects
                .SelectMany(r => r.GetComponentsInChildren<Exhibit.Goods.Implements.Goods>(true))
                .Where(goods => string.IsNullOrEmpty(searchField.value) || goods.Id.StartsWith(searchField.value))
                .ToList();
        }

        static VisualElement GenerateSearchField()
        {
            searchField = new TextField("Idでフィルター(前方一致)");
            searchField.style.paddingTop = 8;
            searchField.style.paddingBottom = 8;
            searchField.style.paddingLeft = 8;
            searchField.style.paddingRight = 8;
            return searchField;
        }

        static VisualElement GenerateGoodsList()
        {
            var goods = GetGoodsFromHierarchy();

            if (goods.Count <= 0)
            {
                var emptyLabel = new Label("一致するIdが見つかりませんでした");
                emptyLabel.style.paddingTop = 8;
                emptyLabel.style.paddingBottom = 8;
                emptyLabel.style.paddingLeft = 8;
                emptyLabel.style.paddingRight = 8;
                return emptyLabel;
            }

            const int cellHeight = 50;

            void BindItem(VisualElement element, int i)
            {
                var labels = element.Children().Select(e => (Label)e).ToArray();
                labels[0].text = $"Name : {goods[i].name}";
                labels[1].text = $"Id : {goods[i].Id}";
            }

            var listView = new ListView(goods, cellHeight, MakeItem, BindItem) { selectionType = SelectionType.Single };
#if UNITY_2021_3_OR_NEWER
            listView.onItemsChosen += SelectGoodsFromHierarchy;
#else
            listView.onItemChosen += SelectGoodsFromHierarchy;
#endif

            listView.style.flexGrow = 1.0f;
            return listView;
        }

        static void SelectGoodsFromHierarchy(Object o)
        {
            var goods = (Exhibit.Goods.Implements.Goods)o;
            Selection.activeObject = goods.gameObject;
        }

        static VisualElement MakeItem()
        {
            var element = new VisualElement();
            element.style.paddingTop = 8;
            element.style.paddingBottom = 8;
            element.style.paddingLeft = 8;
            element.style.paddingRight = 8;

            var nameLabel = new Label();
            var idLabel = new Label();

            element.Add(nameLabel);
            element.Add(idLabel);

            return element;
        }
    }
}
