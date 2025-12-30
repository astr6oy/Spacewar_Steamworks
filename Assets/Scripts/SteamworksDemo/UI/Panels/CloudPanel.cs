using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteamworksDemo.Core;
using SteamworksDemo.Services;

namespace SteamworksDemo.UI.Panels
{
    /// <summary>
    /// Steam Cloud 패널
    /// 클라우드 파일 목록, 저장/로드/삭제 테스트
    /// </summary>
    public class CloudPanel : MonoBehaviour
    {
        [Header("상태 정보")]
        [SerializeField] private Text _cloudStatusText;
        [SerializeField] private Text _quotaText;
        [SerializeField] private Image _quotaBar;

        [Header("파일 목록")]
        [SerializeField] private Transform _fileListContainer;
        [SerializeField] private GameObject _fileItemPrefab;

        [Header("파일 작업")]
        [SerializeField] private InputField _fileNameInput;
        [SerializeField] private InputField _fileContentInput;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private Button _refreshButton;

        [Header("미리보기")]
        [SerializeField] private Text _previewText;

        [Header("색상")]
        [SerializeField] private Color _enabledColor = new Color(0.64f, 0.74f, 0.55f);
        [SerializeField] private Color _disabledColor = new Color(0.75f, 0.38f, 0.42f);

        private void Start()
        {
            // 이벤트 구독
            SteamEventBus.OnCloudFileWritten += OnFileChanged;
            SteamEventBus.OnCloudFileDeleted += OnFileChanged;

            // 버튼 연결
            if (_saveButton != null)
                _saveButton.onClick.AddListener(SaveFile);

            if (_loadButton != null)
                _loadButton.onClick.AddListener(LoadFile);

            if (_deleteButton != null)
                _deleteButton.onClick.AddListener(DeleteFile);

            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(RefreshUI);

            // 기본 파일 이름
            if (_fileNameInput != null)
                _fileNameInput.text = "test_save.txt";

            // 초기 UI
            RefreshUI();
        }

        private void OnDestroy()
        {
            SteamEventBus.OnCloudFileWritten -= OnFileChanged;
            SteamEventBus.OnCloudFileDeleted -= OnFileChanged;
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void OnFileChanged(string fileName)
        {
            RefreshUI();
        }

        /// <summary>
        /// 파일 저장
        /// </summary>
        private void SaveFile()
        {
            string fileName = _fileNameInput?.text;
            string content = _fileContentInput?.text;

            if (string.IsNullOrEmpty(fileName))
            {
                SteamLogger.LogWarning("UI", "파일 이름을 입력하세요");
                return;
            }

            if (string.IsNullOrEmpty(content))
            {
                content = $"테스트 데이터 - {System.DateTime.Now}";
            }

            SteamCloudService.Instance?.WriteFile(fileName, content);
        }

        /// <summary>
        /// 파일 로드
        /// </summary>
        private void LoadFile()
        {
            string fileName = _fileNameInput?.text;

            if (string.IsNullOrEmpty(fileName))
            {
                SteamLogger.LogWarning("UI", "파일 이름을 입력하세요");
                return;
            }

            string content = SteamCloudService.Instance?.ReadFileAsString(fileName);

            if (_previewText != null)
            {
                _previewText.text = content ?? "파일을 찾을 수 없습니다";
            }

            if (_fileContentInput != null && content != null)
            {
                _fileContentInput.text = content;
            }
        }

        /// <summary>
        /// 파일 삭제
        /// </summary>
        private void DeleteFile()
        {
            string fileName = _fileNameInput?.text;

            if (string.IsNullOrEmpty(fileName))
            {
                SteamLogger.LogWarning("UI", "파일 이름을 입력하세요");
                return;
            }

            SteamCloudService.Instance?.DeleteFile(fileName);
        }

        /// <summary>
        /// UI 갱신
        /// </summary>
        private void RefreshUI()
        {
            var service = SteamCloudService.Instance;
            if (service == null) return;

            // Cloud 상태
            if (_cloudStatusText != null)
            {
                bool enabled = service.IsCloudEnabled;
                _cloudStatusText.text = enabled ? "Cloud 활성화" : "Cloud 비활성화";
                _cloudStatusText.color = enabled ? _enabledColor : _disabledColor;
            }

            // 용량 정보
            service.RefreshQuotaInfo();
            if (_quotaText != null)
            {
                string used = SteamCloudService.FormatBytes(service.UsedQuota);
                string total = SteamCloudService.FormatBytes(service.TotalQuota);
                _quotaText.text = $"사용량: {used} / {total}";
            }

            if (_quotaBar != null && service.TotalQuota > 0)
            {
                _quotaBar.fillAmount = (float)service.UsedQuota / service.TotalQuota;
            }

            // 파일 목록
            RefreshFileList();
        }

        /// <summary>
        /// 파일 목록 갱신
        /// </summary>
        private void RefreshFileList()
        {
            if (_fileListContainer == null) return;

            // 기존 아이템 제거
            foreach (Transform child in _fileListContainer)
            {
                Destroy(child.gameObject);
            }

            var service = SteamCloudService.Instance;
            if (service == null) return;

            List<CloudFileInfo> files = service.GetAllFiles();

            if (files.Count == 0)
            {
                SteamLogger.Log("UI", "저장된 파일이 없습니다");
                return;
            }

            foreach (var file in files)
            {
                CreateFileItem(file);
            }
        }

        /// <summary>
        /// 파일 아이템 생성
        /// </summary>
        private void CreateFileItem(CloudFileInfo file)
        {
            if (_fileItemPrefab == null) return;

            GameObject item = Instantiate(_fileItemPrefab, _fileListContainer);

            // 파일 이름
            var nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = file.FileName;
            }

            // 파일 크기
            var sizeText = item.transform.Find("SizeText")?.GetComponent<Text>();
            if (sizeText != null)
            {
                sizeText.text = file.GetFormattedSize();
            }

            // 수정 시간
            var timeText = item.transform.Find("TimeText")?.GetComponent<Text>();
            if (timeText != null)
            {
                timeText.text = file.GetDateTime().ToString("yyyy-MM-dd HH:mm");
            }

            // 선택 버튼
            var selectButton = item.transform.Find("SelectButton")?.GetComponent<Button>();
            if (selectButton != null)
            {
                string fileName = file.FileName;
                selectButton.onClick.AddListener(() =>
                {
                    if (_fileNameInput != null)
                        _fileNameInput.text = fileName;

                    LoadFile();
                });
            }

            // 삭제 버튼
            var deleteButton = item.transform.Find("DeleteButton")?.GetComponent<Button>();
            if (deleteButton != null)
            {
                string fileName = file.FileName;
                deleteButton.onClick.AddListener(() =>
                {
                    SteamCloudService.Instance?.DeleteFile(fileName);
                });
            }
        }
    }
}
