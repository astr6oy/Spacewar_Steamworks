using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteamworksDemo.Core;
using SteamworksDemo.Services;
using Steamworks;

namespace SteamworksDemo.UI.Panels
{
    /// <summary>
    /// P2P 네트워킹 테스트 패널
    /// 패킷 송수신 테스트 및 시각화
    /// </summary>
    public class P2PPanel : MonoBehaviour
    {
        [Header("메시지 전송")]
        [SerializeField] private InputField _messageInput;
        [SerializeField] private Button _broadcastButton;

        [Header("통계")]
        [SerializeField] private Text _sentCountText;
        [SerializeField] private Text _receivedCountText;
        [SerializeField] private Button _resetStatsButton;

        [Header("연결된 피어")]
        [SerializeField] private Transform _peersContainer;
        [SerializeField] private GameObject _peerItemPrefab;

        [Header("수신 로그")]
        [SerializeField] private Transform _logContainer;
        [SerializeField] private GameObject _logItemPrefab;
        [SerializeField] private int _maxLogItems = 20;

        [Header("시각화")]
        [SerializeField] private Transform _visualizationArea;
        [SerializeField] private GameObject _packetVisualPrefab;

        [Header("색상")]
        [SerializeField] private Color _connectedColor = new Color(0.64f, 0.74f, 0.55f);
        [SerializeField] private Color _sendColor = new Color(0.53f, 0.75f, 0.82f);
        [SerializeField] private Color _receiveColor = new Color(0.92f, 0.80f, 0.55f);

        private Queue<GameObject> _logItems = new Queue<GameObject>();

        private void Start()
        {
            // 이벤트 구독
            SteamEventBus.OnP2PPacketReceived += OnPacketReceived;
            SteamEventBus.OnP2PSessionRequested += OnSessionRequested;
            SteamEventBus.OnP2PSessionClosed += OnSessionClosed;

            // 버튼 연결
            if (_broadcastButton != null)
                _broadcastButton.onClick.AddListener(BroadcastMessage);

            if (_resetStatsButton != null)
                _resetStatsButton.onClick.AddListener(ResetStats);
        }

        private void OnDestroy()
        {
            SteamEventBus.OnP2PPacketReceived -= OnPacketReceived;
            SteamEventBus.OnP2PSessionRequested -= OnSessionRequested;
            SteamEventBus.OnP2PSessionClosed -= OnSessionClosed;
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void Update()
        {
            // 실시간 통계 업데이트
            UpdateStats();
        }

        /// <summary>
        /// 메시지 브로드캐스트
        /// </summary>
        private void BroadcastMessage()
        {
            string message = _messageInput?.text;
            if (string.IsNullOrEmpty(message)) return;

            SteamP2PService.Instance?.BroadcastMessage(message);

            // 전송 시각화
            SpawnPacketVisual(_sendColor, Vector3.left);

            // 입력 필드 초기화
            if (_messageInput != null)
                _messageInput.text = "";
        }

        /// <summary>
        /// 통계 초기화
        /// </summary>
        private void ResetStats()
        {
            SteamP2PService.Instance?.ResetStats();
            ClearLog();
        }

        /// <summary>
        /// UI 갱신
        /// </summary>
        private void RefreshUI()
        {
            UpdateStats();
            RefreshPeersList();
        }

        /// <summary>
        /// 통계 업데이트
        /// </summary>
        private void UpdateStats()
        {
            var service = SteamP2PService.Instance;
            if (service == null) return;

            if (_sentCountText != null)
                _sentCountText.text = $"송신: {service.PacketsSent}";

            if (_receivedCountText != null)
                _receivedCountText.text = $"수신: {service.PacketsReceived}";
        }

        /// <summary>
        /// 연결된 피어 목록 갱신
        /// </summary>
        private void RefreshPeersList()
        {
            if (_peersContainer == null) return;

            // 기존 아이템 제거
            foreach (Transform child in _peersContainer)
            {
                Destroy(child.gameObject);
            }

            var service = SteamP2PService.Instance;
            if (service == null) return;

            foreach (var peerId in service.ConnectedPeers)
            {
                CreatePeerItem(peerId);
            }
        }

        /// <summary>
        /// 피어 아이템 생성
        /// </summary>
        private void CreatePeerItem(CSteamID peerId)
        {
            if (_peerItemPrefab == null) return;

            GameObject item = Instantiate(_peerItemPrefab, _peersContainer);

            // 이름
            var nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = SteamFriends.GetFriendPersonaName(peerId);
            }

            // 상태 표시
            var statusImage = item.transform.Find("StatusImage")?.GetComponent<Image>();
            if (statusImage != null)
            {
                bool connected = SteamP2PService.Instance?.IsConnectedTo(peerId) ?? false;
                statusImage.color = connected ? _connectedColor : Color.gray;
            }

            // 전송 버튼
            var sendButton = item.transform.Find("SendButton")?.GetComponent<Button>();
            if (sendButton != null)
            {
                CSteamID targetId = peerId;
                sendButton.onClick.AddListener(() =>
                {
                    string msg = $"테스트 메시지 ({System.DateTime.Now:HH:mm:ss})";
                    SteamP2PService.Instance?.SendMessage(targetId, msg);
                    SpawnPacketVisual(_sendColor, Vector3.left);
                });
            }

            // 연결 종료 버튼
            var closeButton = item.transform.Find("CloseButton")?.GetComponent<Button>();
            if (closeButton != null)
            {
                CSteamID targetId = peerId;
                closeButton.onClick.AddListener(() =>
                {
                    SteamP2PService.Instance?.CloseSession(targetId);
                    RefreshPeersList();
                });
            }
        }

        /// <summary>
        /// 로그 아이템 추가
        /// </summary>
        private void AddLogItem(string message, Color color)
        {
            if (_logContainer == null || _logItemPrefab == null) return;

            GameObject item = Instantiate(_logItemPrefab, _logContainer);
            _logItems.Enqueue(item);

            var text = item.GetComponent<Text>();
            if (text != null)
            {
                text.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
                text.color = color;
            }

            // 최대 개수 초과 시 오래된 항목 제거
            while (_logItems.Count > _maxLogItems)
            {
                var oldItem = _logItems.Dequeue();
                if (oldItem != null)
                {
                    Destroy(oldItem);
                }
            }
        }

        /// <summary>
        /// 로그 초기화
        /// </summary>
        private void ClearLog()
        {
            while (_logItems.Count > 0)
            {
                var item = _logItems.Dequeue();
                if (item != null)
                {
                    Destroy(item);
                }
            }
        }

        /// <summary>
        /// 패킷 시각화 (움직이는 오브젝트)
        /// </summary>
        private void SpawnPacketVisual(Color color, Vector3 direction)
        {
            if (_visualizationArea == null || _packetVisualPrefab == null) return;

            GameObject visual = Instantiate(_packetVisualPrefab, _visualizationArea);

            // 색상 설정
            var image = visual.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }

            // 시작 위치
            var rectTransform = visual.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 중앙에서 시작
                rectTransform.anchoredPosition = Vector2.zero;

                // 이동 애니메이션 (간단한 구현)
                StartCoroutine(MoveAndDestroy(visual, direction * 640f, 1f));
            }
        }

        private System.Collections.IEnumerator MoveAndDestroy(GameObject obj, Vector3 targetOffset, float duration)
        {
            var rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform == null) yield break;

            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(targetOffset.x, targetOffset.y);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

                    // 페이드 아웃
                    var image = obj.GetComponent<Image>();
                    if (image != null)
                    {
                        var c = image.color;
                        c.a = 1f - t;
                        image.color = c;
                    }
                }

                yield return null;
            }

            if (obj != null)
            {
                Destroy(obj);
            }
        }

        #region 이벤트 핸들러

        private void OnPacketReceived(CSteamID senderId, byte[] data)
        {
            string senderName = SteamFriends.GetFriendPersonaName(senderId);
            string message = System.Text.Encoding.UTF8.GetString(data);

            AddLogItem($"{senderName}: {message}", _receiveColor);
            SpawnPacketVisual(_receiveColor, Vector3.right);
            RefreshPeersList();
        }

        private void OnSessionRequested(CSteamID remoteId)
        {
            string name = SteamFriends.GetFriendPersonaName(remoteId);
            AddLogItem($"세션 요청: {name}", Color.white);
            RefreshPeersList();
        }

        private void OnSessionClosed(CSteamID remoteId)
        {
            string name = SteamFriends.GetFriendPersonaName(remoteId);
            AddLogItem($"세션 종료: {name}", Color.gray);
            RefreshPeersList();
        }

        #endregion
    }
}
