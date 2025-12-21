using UnityEngine;
using System.Text; // Required for StringBuilder

public class HoverInfo : MonoBehaviour
{
    [Header("Manual Overrides")]
    public string objectName = ""; // Leave empty to Auto-Detect
    [TextArea] public string staticDescription = ""; // Leave empty to Auto-Detect

    // References to components (Lazy loaded)
    private BuildingBase _building;
    private UniversalProcessor _processor;
    private DeckDispenser _dispenser;
    private Unpacker _unpacker;

    private void Awake()
    {
        // Cache references so we don't GetComponent every frame
        _building = GetComponent<BuildingBase>();
        _processor = GetComponent<UniversalProcessor>();
        _dispenser = GetComponent<DeckDispenser>();
        _unpacker = GetComponent<Unpacker>();

        // Auto-Name if empty
        if (string.IsNullOrEmpty(objectName))
        {
            objectName = gameObject.name.Replace("(Clone)", "").Trim();
        }
    }

    public virtual string GetDynamicContent()
    {
        StringBuilder sb = new StringBuilder();

        // 1. Add manual description if it exists
        if (!string.IsNullOrEmpty(staticDescription))
        {
            sb.AppendLine(staticDescription);
            sb.AppendLine("---");
        }

        // 2. Add Building Generic Info
        if (_building != null)
        {
            if (_building.internalItem != null)
            {
                sb.AppendLine($"<b>Holding:</b> {_building.internalItem.GetDebugLabel()}");
            }
            else
            {
                sb.AppendLine("<i>Empty</i>");
            }
        }

        // 3. Add Processor Info
        if (_processor != null)
        {
            if (_processor.activeRecipe != null)
                sb.AppendLine($"<color=yellow>Recipe:</color> {_processor.activeRecipe.name}");
            else
                sb.AppendLine("<color=red>No Recipe</color>");

            sb.AppendLine($"Status: {(_processor.activeRecipe != null ? "Working" : "Idle")}");
        }

        // 4. Add Dispenser Info
        if (_dispenser != null)
        {
            sb.AppendLine($"<b>Output:</b> {_dispenser.outputRank} of {_dispenser.outputSuit}");
            sb.AppendLine($"<b>Status:</b> {_dispenser.lastStatus}");
        }

        // 5. Add Unpacker Info
        if (_unpacker != null)
        {
            sb.AppendLine("<color=orange>Unpacking Supply Drop</color>");
            sb.AppendLine($"Speed: {_unpacker.productionSpeed}s");
        }

        return sb.ToString();
    }
}