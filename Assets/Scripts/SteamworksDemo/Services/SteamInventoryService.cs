using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using SteamworksDemo.Core;

namespace SteamworksDemo.Services
{
    /// <summary>
    /// Steam Inventory 서비스
    /// 인벤토리 아이템 조회 및 관리
    /// SpaceWar는 제한된 인벤토리 테스트만 가능
    /// </summary>
    public class SteamInventoryService : MonoBehaviour, ISteamService
    {
        public static SteamInventoryService Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        // 인벤토리 아이템 목록
        private readonly List<InventoryItemInfo> _items = new List<InventoryItemInfo>();
        public IReadOnlyList<InventoryItemInfo> Items => _items;

        // 아이템 정의 목록
        private readonly List<SteamItemDef_t> _itemDefinitions = new List<SteamItemDef_t>();
        public IReadOnlyList<SteamItemDef_t> ItemDefinitions => _itemDefinitions;

        // 현재 결과 핸들
        private SteamInventoryResult_t _currentResult = SteamInventoryResult_t.Invalid;

        // Callbacks
        private Callback<SteamInventoryResultReady_t> _inventoryResultReadyCallback;
        private Callback<SteamInventoryDefinitionUpdate_t> _inventoryDefinitionUpdateCallback;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize()
        {
            if (IsInitialized) return;
            if (!SteamManager.CheckInitialized("InventoryService")) return;

            // Callbacks 등록
            _inventoryResultReadyCallback = Callback<SteamInventoryResultReady_t>.Create(OnInventoryResultReady);
            _inventoryDefinitionUpdateCallback = Callback<SteamInventoryDefinitionUpdate_t>.Create(OnInventoryDefinitionUpdate);

            // 아이템 정의 로드
            LoadItemDefinitions();

            IsInitialized = true;
            SteamLogger.Log("Inventory", "InventoryService 초기화 완료");
        }

        /// <summary>
        /// 아이템 정의 로드
        /// </summary>
        public void LoadItemDefinitions()
        {
            if (!SteamManager.CheckInitialized("InventoryService")) return;

            // 아이템 정의 ID 가져오기
            uint count = 0;
            if (SteamInventory.GetItemDefinitionIDs(null, ref count))
            {
                SteamItemDef_t[] definitions = new SteamItemDef_t[count];
                if (SteamInventory.GetItemDefinitionIDs(definitions, ref count))
                {
                    _itemDefinitions.Clear();
                    _itemDefinitions.AddRange(definitions);
                    SteamLogger.Log("Inventory", $"{count}개 아이템 정의 로드");
                }
            }
        }

        /// <summary>
        /// 전체 인벤토리 조회
        /// </summary>
        public void GetAllItems()
        {
            if (!SteamManager.CheckInitialized("InventoryService")) return;

            // 이전 결과 정리
            if (_currentResult != SteamInventoryResult_t.Invalid)
            {
                SteamInventory.DestroyResult(_currentResult);
            }

            bool success = SteamInventory.GetAllItems(out _currentResult);
            SteamLogger.LogResult("Inventory", "인벤토리 조회 요청", success);
        }

        /// <summary>
        /// 아이템 정의 속성 가져오기
        /// </summary>
        public string GetItemProperty(SteamItemDef_t itemDef, string propertyName)
        {
            if (!SteamManager.CheckInitialized("InventoryService")) return "";

            uint bufferSize = 256;
            string buffer;

            if (SteamInventory.GetItemDefinitionProperty(itemDef, propertyName, out buffer, ref bufferSize))
            {
                return buffer;
            }

            return "";
        }

        /// <summary>
        /// 아이템 이름 가져오기
        /// </summary>
        public string GetItemName(SteamItemDef_t itemDef)
        {
            return GetItemProperty(itemDef, "name");
        }

        /// <summary>
        /// 아이템 설명 가져오기
        /// </summary>
        public string GetItemDescription(SteamItemDef_t itemDef)
        {
            return GetItemProperty(itemDef, "description");
        }

        /// <summary>
        /// 아이템 가격 가져오기
        /// </summary>
        public string GetItemPrice(SteamItemDef_t itemDef)
        {
            return GetItemProperty(itemDef, "price");
        }

        /// <summary>
        /// 프로모 아이템 지급 (테스트용)
        /// </summary>
        public void GrantPromoItems()
        {
            if (!SteamManager.CheckInitialized("InventoryService")) return;

            if (_currentResult != SteamInventoryResult_t.Invalid)
            {
                SteamInventory.DestroyResult(_currentResult);
            }

            bool success = SteamInventory.GrantPromoItems(out _currentResult);
            SteamLogger.LogResult("Inventory", "프로모 아이템 지급 요청", success);
        }

        /// <summary>
        /// 아이템 생성 (테스트용 - 개발자 모드에서만 작동)
        /// </summary>
        public void GenerateItems(SteamItemDef_t[] itemDefs, uint[] quantities)
        {
            if (!SteamManager.CheckInitialized("InventoryService")) return;

            if (_currentResult != SteamInventoryResult_t.Invalid)
            {
                SteamInventory.DestroyResult(_currentResult);
            }

            bool success = SteamInventory.GenerateItems(out _currentResult, itemDefs, quantities, (uint)itemDefs.Length);
            SteamLogger.LogResult("Inventory", "아이템 생성 요청", success);
        }

        #region Callbacks

        private void OnInventoryResultReady(SteamInventoryResultReady_t result)
        {
            if (result.m_result != EResult.k_EResultOK)
            {
                SteamLogger.LogError("Inventory", $"인벤토리 결과 실패: {result.m_result}");
                return;
            }

            ProcessInventoryResult(result.m_handle);
            SteamEventBus.RaiseInventoryUpdated();
        }

        private void OnInventoryDefinitionUpdate(SteamInventoryDefinitionUpdate_t result)
        {
            SteamLogger.Log("Inventory", "아이템 정의 업데이트됨");
            LoadItemDefinitions();
        }

        #endregion

        /// <summary>
        /// 인벤토리 결과 처리
        /// </summary>
        private void ProcessInventoryResult(SteamInventoryResult_t handle)
        {
            _items.Clear();

            uint count = 0;
            if (!SteamInventory.GetResultItems(handle, null, ref count) || count == 0)
            {
                SteamLogger.Log("Inventory", "인벤토리가 비어있습니다");
                return;
            }

            SteamItemDetails_t[] details = new SteamItemDetails_t[count];
            if (SteamInventory.GetResultItems(handle, details, ref count))
            {
                foreach (var detail in details)
                {
                    _items.Add(new InventoryItemInfo
                    {
                        ItemId = detail.m_itemId,
                        Definition = detail.m_iDefinition,
                        Quantity = detail.m_unQuantity,
                        Flags = detail.m_unFlags,
                        Name = GetItemName(detail.m_iDefinition),
                        Description = GetItemDescription(detail.m_iDefinition)
                    });
                }

                SteamLogger.Log("Inventory", $"{_items.Count}개 아이템 로드됨");
            }
        }

        public void Shutdown()
        {
            if (_currentResult != SteamInventoryResult_t.Invalid)
            {
                SteamInventory.DestroyResult(_currentResult);
                _currentResult = SteamInventoryResult_t.Invalid;
            }

            _items.Clear();
            _itemDefinitions.Clear();
            IsInitialized = false;
            SteamLogger.Log("Inventory", "InventoryService 종료");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    /// <summary>
    /// 인벤토리 아이템 정보
    /// </summary>
    public class InventoryItemInfo
    {
        public SteamItemInstanceID_t ItemId { get; set; }
        public SteamItemDef_t Definition { get; set; }
        public ushort Quantity { get; set; }
        public ushort Flags { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public bool IsNoTrade => (Flags & (ushort)ESteamItemFlags.k_ESteamItemNoTrade) != 0;
        public bool IsRemoved => (Flags & (ushort)ESteamItemFlags.k_ESteamItemRemoved) != 0;
        public bool IsConsumed => (Flags & (ushort)ESteamItemFlags.k_ESteamItemConsumed) != 0;
    }
}
