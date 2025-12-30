using UnityEngine;
using UnityEngine.UI;
using SteamworksDemo.Core;
using SteamworksDemo.Services;

namespace SteamworksDemo.UI.Panels
{
    /// <summary>
    /// Steam Inventory 패널
    /// 인벤토리 아이템 표시 및 관리
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        [Header("정보 표시")]
        [SerializeField] private Text _itemCountText;
        [SerializeField] private Text _definitionCountText;

        [Header("버튼")]
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _grantPromoButton;

        [Header("아이템 그리드")]
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private GameObject _itemPrefab;

        [Header("아이템 정의 목록")]
        [SerializeField] private Transform _definitionsContainer;
        [SerializeField] private GameObject _definitionPrefab;

        [Header("선택된 아이템 상세")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private Text _detailNameText;
        [SerializeField] private Text _detailDescText;
        [SerializeField] private Text _detailQuantityText;

        [Header("색상")]
        [SerializeField] private Color _commonColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color _rareColor = new Color(0.53f, 0.75f, 0.82f);
        [SerializeField] private Color _epicColor = new Color(0.6f, 0.4f, 0.8f);

        private void Start()
        {
            // 이벤트 구독
            SteamEventBus.OnInventoryUpdated += OnInventoryUpdated;

            // 버튼 연결
            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(RefreshInventory);

            if (_grantPromoButton != null)
                _grantPromoButton.onClick.AddListener(GrantPromoItems);

            // 상세 패널 숨기기
            if (_detailPanel != null)
                _detailPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            SteamEventBus.OnInventoryUpdated -= OnInventoryUpdated;
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void OnInventoryUpdated()
        {
            RefreshUI();
        }

        /// <summary>
        /// 인벤토리 새로고침
        /// </summary>
        private void RefreshInventory()
        {
            SteamInventoryService.Instance?.GetAllItems();
        }

        /// <summary>
        /// 프로모 아이템 지급
        /// </summary>
        private void GrantPromoItems()
        {
            SteamInventoryService.Instance?.GrantPromoItems();
        }

        /// <summary>
        /// UI 갱신
        /// </summary>
        private void RefreshUI()
        {
            var service = SteamInventoryService.Instance;
            if (service == null) return;

            // 카운트 표시
            if (_itemCountText != null)
                _itemCountText.text = $"보유 아이템: {service.Items.Count}개";

            if (_definitionCountText != null)
                _definitionCountText.text = $"아이템 정의: {service.ItemDefinitions.Count}개";

            // 목록 갱신
            RefreshItemsGrid();
            RefreshDefinitionsList();
        }

        /// <summary>
        /// 아이템 그리드 갱신
        /// </summary>
        private void RefreshItemsGrid()
        {
            if (_itemsContainer == null) return;

            // 기존 아이템 제거
            foreach (Transform child in _itemsContainer)
            {
                Destroy(child.gameObject);
            }

            var service = SteamInventoryService.Instance;
            if (service == null) return;

            foreach (var item in service.Items)
            {
                CreateItemSlot(item);
            }
        }

        /// <summary>
        /// 아이템 슬롯 생성
        /// </summary>
        private void CreateItemSlot(InventoryItemInfo item)
        {
            if (_itemPrefab == null) return;

            GameObject slot = Instantiate(_itemPrefab, _itemsContainer);

            // 배경 (희귀도에 따라 색상)
            var background = slot.GetComponent<Image>();
            if (background != null)
            {
                background.color = GetRarityColor(item);
            }

            // 아이콘 (기본 도형)
            var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                // 아이템 정의 ID 기반으로 색상 지정
                icon.color = GetItemColor(item.Definition.m_SteamItemDef);
            }

            // 이름
            var nameText = slot.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(item.Name) ? $"Item #{item.Definition}" : item.Name;
            }

            // 수량
            var quantityText = slot.transform.Find("QuantityText")?.GetComponent<Text>();
            if (quantityText != null)
            {
                quantityText.text = item.Quantity > 1 ? $"x{item.Quantity}" : "";
            }

            // 클릭 이벤트
            var button = slot.GetComponent<Button>();
            if (button != null)
            {
                var itemCopy = item;
                button.onClick.AddListener(() => ShowItemDetail(itemCopy));
            }
        }

        /// <summary>
        /// 아이템 정의 목록 갱신
        /// </summary>
        private void RefreshDefinitionsList()
        {
            if (_definitionsContainer == null) return;

            // 기존 아이템 제거
            foreach (Transform child in _definitionsContainer)
            {
                Destroy(child.gameObject);
            }

            var service = SteamInventoryService.Instance;
            if (service == null) return;

            foreach (var def in service.ItemDefinitions)
            {
                CreateDefinitionItem(def);
            }
        }

        /// <summary>
        /// 아이템 정의 아이템 생성
        /// </summary>
        private void CreateDefinitionItem(Steamworks.SteamItemDef_t def)
        {
            if (_definitionPrefab == null) return;

            GameObject item = Instantiate(_definitionPrefab, _definitionsContainer);

            var service = SteamInventoryService.Instance;

            // ID
            var idText = item.transform.Find("IdText")?.GetComponent<Text>();
            if (idText != null)
            {
                idText.text = $"#{def.m_SteamItemDef}";
            }

            // 이름
            var nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                string name = service?.GetItemName(def);
                nameText.text = string.IsNullOrEmpty(name) ? "Unknown" : name;
            }

            // 가격
            var priceText = item.transform.Find("PriceText")?.GetComponent<Text>();
            if (priceText != null)
            {
                string price = service?.GetItemPrice(def);
                priceText.text = string.IsNullOrEmpty(price) ? "-" : price;
            }
        }

        /// <summary>
        /// 아이템 상세 표시
        /// </summary>
        private void ShowItemDetail(InventoryItemInfo item)
        {
            if (_detailPanel == null) return;

            _detailPanel.SetActive(true);

            if (_detailNameText != null)
                _detailNameText.text = string.IsNullOrEmpty(item.Name) ? $"Item #{item.Definition}" : item.Name;

            if (_detailDescText != null)
                _detailDescText.text = string.IsNullOrEmpty(item.Description) ? "설명 없음" : item.Description;

            if (_detailQuantityText != null)
                _detailQuantityText.text = $"수량: {item.Quantity}";
        }

        /// <summary>
        /// 희귀도에 따른 색상
        /// </summary>
        private Color GetRarityColor(InventoryItemInfo item)
        {
            // 간단한 희귀도 구분 (실제로는 아이템 속성에서 가져와야 함)
            int id = item.Definition.m_SteamItemDef;

            if (id % 10 == 0) return _epicColor;
            if (id % 5 == 0) return _rareColor;
            return _commonColor;
        }

        /// <summary>
        /// 아이템 ID 기반 색상
        /// </summary>
        private Color GetItemColor(int itemDefId)
        {
            // ID를 기반으로 색상 생성
            float hue = (itemDefId * 0.1f) % 1f;
            return Color.HSVToRGB(hue, 0.6f, 0.8f);
        }
    }
}
