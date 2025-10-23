using S1API.PhoneApp;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace S1DockExports
{
    public class DockExportsApp : PhoneAppBase
    {
        private DockExportsMod mod;
        private GameObject appUI;
        private TabType currentTab = TabType.CreateShipment;

        // UI References
        private GameObject createShipmentPanel;
        private GameObject activeShipmentsPanel;
        private GameObject historyPanel;

        // Tab buttons
        private Button createTabButton;
        private Button activeTabButton;
        private Button historyTabButton;

        // Create Shipment UI
        private TMP_Dropdown routeDropdown;
        private TMP_InputField quantityInput;
        private TMP_Text brickPriceText;
        private TMP_Text totalValueText;
        private TMP_Text infoText;
        private Button confirmButton;

        // Active Shipments UI
        private TMP_Text activeShipmentText;
        private TMP_Text progressText;
        private TMP_Text nextPayoutText;

        // History UI
        private TMP_Text historyText;
        private ScrollRect historyScrollView;

        public DockExportsApp(DockExportsMod modInstance)
        {
            mod = modInstance;
        }

        public override string AppName => "Dock Exports";
        public override string AppID => "dock_exports";
        public override Sprite AppIcon => LoadIcon(); // You'll need to provide an icon

        public override void OnAppOpen()
        {
            if (appUI == null)
            {
                CreateUI();
            }
            appUI.SetActive(true);
            RefreshCurrentTab();
        }

        public override void OnAppClose()
        {
            if (appUI != null)
            {
                appUI.SetActive(false);
            }
        }

        private Sprite LoadIcon()
        {
            // TODO: Load your custom icon from embedded resources or assets
            // For now, return null and the game will use a default icon
            return null;
        }

        private void CreateUI()
        {
            // Create main container
            appUI = new GameObject("DockExportsUI");
            Canvas canvas = appUI.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = appUI.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            appUI.AddComponent<GraphicRaycaster>();

            // Create background
            GameObject background = CreateUIElement("Background", appUI.transform);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.15f, 0.2f, 0.95f); // Dark blue-grey
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Create header
            CreateHeader(appUI.transform);

            // Create tab buttons
            CreateTabButtons(appUI.transform);

            // Create panels for each tab
            CreateTabPanels(appUI.transform);

            // Initially show create shipment tab
            SwitchTab(TabType.CreateShipment);

            appUI.SetActive(false);
        }

        private void CreateHeader(Transform parent)
        {
            GameObject header = CreateUIElement("Header", parent);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 100);

            Image headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.05f, 0.1f, 0.15f, 1f);

            // Title
            GameObject titleObj = CreateUIElement("Title", header.transform);
            TMP_Text title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "DOCK EXPORTS";
            title.fontSize = 36;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.7f, 0.85f, 1f, 1f);
            title.alignment = TextAlignmentOptions.Center;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.sizeDelta = Vector2.zero;

            // Subtitle
            GameObject subtitleObj = CreateUIElement("Subtitle", header.transform);
            TMP_Text subtitle = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitle.text = "Overseas Trade Operations";
            subtitle.fontSize = 18;
            subtitle.color = new Color(0.5f, 0.6f, 0.7f, 1f);
            subtitle.alignment = TextAlignmentOptions.Center;

            RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0, 0);
            subtitleRect.anchorMax = new Vector2(1, 0);
            subtitleRect.pivot = new Vector2(0.5f, 0);
            subtitleRect.anchoredPosition = new Vector2(0, 10);
            subtitleRect.sizeDelta = new Vector2(0, 30);
        }

        private void CreateTabButtons(Transform parent)
        {
            GameObject tabContainer = CreateUIElement("TabButtons", parent);
            RectTransform tabRect = tabContainer.GetComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0, 1);
            tabRect.anchorMax = new Vector2(1, 1);
            tabRect.pivot = new Vector2(0.5f, 1);
            tabRect.anchoredPosition = new Vector2(0, -100);
            tabRect.sizeDelta = new Vector2(0, 60);

            // Create three tab buttons
            createTabButton = CreateTabButton("Create Shipment", tabContainer.transform, 0, TabType.CreateShipment);
            activeTabButton = CreateTabButton("Active", tabContainer.transform, 1, TabType.ActiveShipments);
            historyTabButton = CreateTabButton("History", tabContainer.transform, 2, TabType.History);
        }

        private Button CreateTabButton(string label, Transform parent, int index, TabType tabType)
        {
            GameObject btnObj = CreateUIElement($"Tab_{label}", parent);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();

            float width = 1f / 3f;
            btnRect.anchorMin = new Vector2(width * index, 0);
            btnRect.anchorMax = new Vector2(width * (index + 1), 1);
            btnRect.sizeDelta = Vector2.zero;

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.15f, 0.2f, 0.25f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            btn.onClick.AddListener(() => SwitchTab(tabType));

            // Button text
            GameObject textObj = CreateUIElement("Text", btnObj.transform);
            TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btn;
        }

        private void CreateTabPanels(Transform parent)
        {
            // Create Shipment Panel
            createShipmentPanel = CreatePanel("CreateShipmentPanel", parent);
            CreateShipmentUI(createShipmentPanel.transform);

            // Active Shipments Panel
            activeShipmentsPanel = CreatePanel("ActiveShipmentsPanel", parent);
            CreateActiveShipmentsUI(activeShipmentsPanel.transform);

            // History Panel
            historyPanel = CreatePanel("HistoryPanel", parent);
            CreateHistoryUI(historyPanel.transform);
        }

        private GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = CreateUIElement(name, parent);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.offsetMin = new Vector2(20, 20);
            panelRect.offsetMax = new Vector2(-20, -180);

            return panel;
        }

        private void CreateShipmentUI(Transform parent)
        {
            // Route selection
            GameObject routeLabel = CreateLabel("Route:", parent, new Vector2(0, -50), 24);

            GameObject dropdownObj = CreateUIElement("RouteDropdown", parent);
            RectTransform dropdownRect = dropdownObj.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0.5f, 1);
            dropdownRect.anchorMax = new Vector2(0.5f, 1);
            dropdownRect.pivot = new Vector2(0.5f, 1);
            dropdownRect.anchoredPosition = new Vector2(0, -90);
            dropdownRect.sizeDelta = new Vector2(600, 50);

            routeDropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            routeDropdown.options.Add(new TMP_Dropdown.OptionData("Wholesale (Safe, Instant)"));
            routeDropdown.options.Add(new TMP_Dropdown.OptionData("Consignment (Risky, 4 Weeks)"));
            routeDropdown.onValueChanged.AddListener(OnRouteChanged);

            // Quantity input
            GameObject qtyLabel = CreateLabel("Quantity (Bricks):", parent, new Vector2(0, -160), 24);

            GameObject inputObj = CreateUIElement("QuantityInput", parent);
            RectTransform inputRect = inputObj.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 1);
            inputRect.anchorMax = new Vector2(0.5f, 1);
            inputRect.pivot = new Vector2(0.5f, 1);
            inputRect.anchoredPosition = new Vector2(0, -200);
            inputRect.sizeDelta = new Vector2(600, 50);

            Image inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.2f, 0.25f, 0.3f, 1f);

            quantityInput = inputObj.AddComponent<TMP_InputField>();
            quantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            quantityInput.text = "50";
            quantityInput.onValueChanged.AddListener(OnQuantityChanged);

            // Info display
            brickPriceText = CreateLabel("Brick Price: $14,700", parent, new Vector2(0, -280), 20).GetComponent<TextMeshProUGUI>();
            totalValueText = CreateLabel("Total Value: $735,000", parent, new Vector2(0, -320), 24).GetComponent<TextMeshProUGUI>();
            totalValueText.fontStyle = FontStyles.Bold;
            totalValueText.color = new Color(0.4f, 1f, 0.4f, 1f);

            infoText = CreateLabel("Safe payment. Instant transfer. 30-day cooldown.", parent, new Vector2(0, -380), 18).GetComponent<TextMeshProUGUI>();
            infoText.color = new Color(0.7f, 0.8f, 0.9f, 1f);

            // Confirm button
            GameObject btnObj = CreateUIElement("ConfirmButton", parent);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0);
            btnRect.anchorMax = new Vector2(0.5f, 0);
            btnRect.pivot = new Vector2(0.5f, 0);
            btnRect.anchoredPosition = new Vector2(0, 50);
            btnRect.sizeDelta = new Vector2(400, 60);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.6f, 0.3f, 1f);

            confirmButton = btnObj.AddComponent<Button>();
            confirmButton.targetGraphic = btnImage;
            confirmButton.onClick.AddListener(OnConfirmShipment);

            GameObject btnTextObj = CreateUIElement("Text", btnObj.transform);
            TMP_Text btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "CONFIRM SHIPMENT";
            btnText.fontSize = 24;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;

            RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
        }

        private void CreateActiveShipmentsUI(Transform parent)
        {
            activeShipmentText = CreateLabel("No active shipments", parent, new Vector2(0, -100), 24).GetComponent<TextMeshProUGUI>();
            activeShipmentText.alignment = TextAlignmentOptions.Center;

            progressText = CreateLabel("", parent, new Vector2(0, -200), 20).GetComponent<TextMeshProUGUI>();
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.color = new Color(0.7f, 0.9f, 1f, 1f);

            nextPayoutText = CreateLabel("", parent, new Vector2(0, -250), 18).GetComponent<TextMeshProUGUI>();
            nextPayoutText.alignment = TextAlignmentOptions.Center;
            nextPayoutText.color = new Color(0.9f, 0.9f, 0.6f, 1f);
        }

        private void CreateHistoryUI(Transform parent)
        {
            historyText = CreateLabel("Shipment history will appear here", parent, new Vector2(0, -100), 20).GetComponent<TextMeshProUGUI>();
            historyText.alignment = TextAlignmentOptions.TopLeft;
        }

        private GameObject CreateLabel(string text, Transform parent, Vector2 position, int fontSize)
        {
            GameObject labelObj = CreateUIElement("Label", parent);
            TMP_Text label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1);
            labelRect.anchorMax = new Vector2(0.5f, 1);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.anchoredPosition = position;
            labelRect.sizeDelta = new Vector2(800, 40);

            return labelObj;
        }

        private GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private void SwitchTab(TabType tab)
        {
            currentTab = tab;

            // Hide all panels
            createShipmentPanel.SetActive(false);
            activeShipmentsPanel.SetActive(false);
            historyPanel.SetActive(false);

            // Update button colors
            UpdateTabButtonColor(createTabButton, tab == TabType.CreateShipment);
            UpdateTabButtonColor(activeTabButton, tab == TabType.ActiveShipments);
            UpdateTabButtonColor(historyTabButton, tab == TabType.History);

            // Show selected panel
            switch (tab)
            {
                case TabType.CreateShipment:
                    createShipmentPanel.SetActive(true);
                    RefreshCreateShipmentTab();
                    break;
                case TabType.ActiveShipments:
                    activeShipmentsPanel.SetActive(true);
                    RefreshActiveShipmentsTab();
                    break;
                case TabType.History:
                    historyPanel.SetActive(true);
                    RefreshHistoryTab();
                    break;
            }
        }

        private void UpdateTabButtonColor(Button button, bool active)
        {
            Image img = button.GetComponent<Image>();
            img.color = active
                ? new Color(0.3f, 0.5f, 0.7f, 1f)
                : new Color(0.15f, 0.2f, 0.25f, 1f);
        }

        private void RefreshCurrentTab()
        {
            SwitchTab(currentTab);
        }

        private void RefreshCreateShipmentTab()
        {
            OnQuantityChanged(quantityInput.text);
        }

        private void RefreshActiveShipmentsTab()
        {
            ShipmentData shipment = mod.GetActiveShipment();

            if (shipment == null || shipment.Completed)
            {
                activeShipmentText.text = "No active shipments";
                progressText.text = "";
                nextPayoutText.text = "";
                return;
            }

            activeShipmentText.text = $"{shipment.Type} Shipment\n{shipment.Quantity} bricks @ ${shipment.TotalValue:N0} total";
            progressText.text = $"Week {shipment.WeeksPaid}/{4}\nPaid so far: ${shipment.TotalPaid:N0}";

            int remaining = 4 - shipment.WeeksPaid;
            nextPayoutText.text = remaining > 0 ? $"Next payout: This Friday ({remaining} weeks remaining)" : "Shipment complete!";
        }

        private void RefreshHistoryTab()
        {
            List<ShipmentHistoryEntry> history = mod.GetHistory();

            if (history == null || history.Count == 0)
            {
                historyText.text = "No shipment history yet.\n\nYour completed deals will appear here.";
                return;
            }

            // Build history display
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("SHIPMENT HISTORY\n");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            // Show most recent first
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var entry = history[i];
                sb.AppendLine(entry.ToString());
                sb.AppendLine();
            }

            // Calculate totals
            int totalShipments = history.Count;
            int totalEarnings = history.Sum(h => h.TotalPaid);
            int consignmentCount = history.Count(h => h.Type == ShipmentType.Consignment);
            int wholesaleCount = history.Count(h => h.Type == ShipmentType.Wholesale);

            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"\nTOTAL SHIPMENTS: {totalShipments}");
            sb.AppendLine($"  Wholesale: {wholesaleCount}");
            sb.AppendLine($"  Consignment: {consignmentCount}");
            sb.AppendLine($"\nTOTAL EARNINGS: ${totalEarnings:N0}");

            historyText.text = sb.ToString();
        }

        private void OnRouteChanged(int index)
        {
            OnQuantityChanged(quantityInput.text);
        }

        private void OnQuantityChanged(string value)
        {
            if (!int.TryParse(value, out int quantity))
            {
                quantity = 0;
            }

            // Get current brick price
            int brickPrice = PriceHelper.GetCurrentBrickPrice();

            bool isWholesale = routeDropdown.value == 0;
            int maxCap = isWholesale ? DockExportsConfig.WHOLESALE_CAP : DockExportsConfig.CONSIGNMENT_CAP;

            // Clamp quantity
            if (quantity > maxCap)
            {
                quantity = maxCap;
                quantityInput.text = maxCap.ToString();
            }

            // Calculate values
            int displayPrice = isWholesale
                ? brickPrice
                : (int)(brickPrice * DockExportsConfig.CONSIGNMENT_MULTIPLIER);
            int totalValue = quantity * displayPrice;

            // Update UI
            brickPriceText.text = $"Brick Price: ${displayPrice:N0}";
            totalValueText.text = $"Total Value: ${totalValue:N0}";

            // Update info text
            if (isWholesale)
            {
                bool onCooldown = mod.IsWholesaleOnCooldown();
                if (onCooldown)
                {
                    int daysLeft = mod.GetWholesaleDaysRemaining();
                    infoText.text = $"⚠ Wholesale on cooldown ({daysLeft} days remaining)";
                    infoText.color = DockExportsConfig.COLOR_WARNING;
                    confirmButton.interactable = false;
                }
                else
                {
                    infoText.text = $"Safe payment. Instant transfer. Max: {maxCap} bricks.\n{DockExportsConfig.WHOLESALE_COOLDOWN_DAYS}-day cooldown after use.";
                    infoText.color = new Color(0.7f, 0.8f, 0.9f, 1f);
                    confirmButton.interactable = quantity > 0;
                }
            }
            else
            {
                ShipmentData active = mod.GetActiveShipment();
                if (active != null && !active.Completed)
                {
                    infoText.text = "⚠ You already have an active consignment";
                    infoText.color = DockExportsConfig.COLOR_WARNING;
                    confirmButton.interactable = false;
                }
                else
                {
                    float expectedPayout = totalValue * (1f - DockExportsConfig.WEEKLY_LOSS_CHANCE * 0.375f); // Rough average
                    int floorValue = quantity * brickPrice;
                    infoText.text = $"{DockExportsConfig.CONSIGNMENT_MULTIPLIER}× price. {DockExportsConfig.INSTALLMENTS} weekly payouts. {DockExportsConfig.WEEKLY_LOSS_CHANCE * 100}% loss risk per week. Max: {maxCap} bricks.\nExpected: ${expectedPayout:N0} | Protected floor: ${floorValue:N0}";
                    infoText.color = new Color(0.7f, 0.8f, 0.9f, 1f);
                    confirmButton.interactable = quantity > 0;
                }
            }
        }

        private void OnConfirmShipment()
        {
            if (!int.TryParse(quantityInput.text, out int quantity) || quantity <= 0)
            {
                return;
            }

            // Get current brick price
            int brickPrice = PriceHelper.GetCurrentBrickPrice();

            bool isWholesale = routeDropdown.value == 0;

            if (isWholesale)
            {
                mod.CreateWholesaleShipment(quantity, brickPrice);
            }
            else
            {
                mod.CreateConsignmentShipment(quantity, brickPrice);
            }

            // Switch to active shipments tab to show result
            SwitchTab(TabType.ActiveShipments);
        }

        private enum TabType
        {
            CreateShipment,
            ActiveShipments,
            History
        }
    }
}