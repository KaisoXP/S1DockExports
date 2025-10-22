using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // <-- required for UnityAction
using S1API.PhoneApp;
using S1API.UI;

namespace S1DockExports
{
    public class HelloWorldApp : PhoneApp
    {
        protected override string AppName => "DockExports";
        protected override string AppTitle => "Dock Exports (Dev)";
        protected override string IconLabel => "DE";
        protected override string IconFileName => "DE.png";

        protected override void OnCreatedUI(GameObject container)
        {
            var panel = UIFactory.Panel("DE_MainPanel", container.transform, new Color(0.09f, 0.09f, 0.09f), fullAnchor: true);
            UIFactory.VerticalLayoutOnGO(panel);
            UIFactory.Text("DE_Title", "Dock Exports Dev", panel.transform, 20, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Text("DE_Info", "Click the button to write to the MelonLoader console.", panel.transform, 14, TextAnchor.UpperLeft);

            var row = UIFactory.ButtonRow("DE_Row", panel.transform, spacing: 8);
            var (_, btnGo, _) = UIFactory.RoundedButtonWithLabel("DE_LogBtn", "Log Test", row.transform, new Color(0.20f, 0.60f, 0.20f), 160, 40, 16, Color.white);

            // UnityAction comes from UnityEngine.Events
            btnGo.GetComponent<Button>().onClick.AddListener((UnityAction)(() =>
            {
                MelonLoader.MelonLogger.Msg("[DockExports] Hello from the Phone App.");
            }));
        }
    }
}
