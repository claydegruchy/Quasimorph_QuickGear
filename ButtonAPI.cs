using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuasimorphHelloWorld
{
	/// <summary>
	/// MonoBehaviour that stores button metadata directly on the button GameObject
	/// so it can be retrieved later (e.g. by an update loop checking Mode).
	/// </summary>
	public class ButtonUserData : MonoBehaviour
	{
		public System.Action Callback { get; set; }
		public string Tooltip { get; set; }
		public object Mode { get; set; }
		public string ExtraData { get; set; }
	}

	/// <summary>
	/// Factory method to create a functional UI button with callback and positioning data.
	///
	/// Usage example:
	/// CreateButton(parent, "myButton", "Click Me", new Vector2(100f, 50f), 80f, 24f, () => { /* callback */ });
	/// </summary>
	/// <param name="parent">The parent GameObject to attach the button under</param>
	/// <param name="buttonId">Unique identifier for this button (used in naming)</param>
	/// <param name="text">The text/caption to display on the button</param>
	/// <param name="positionOffset">Vector2 offset from base position (null = anchoredPosition of zero)</param>
	/// <param name="width">Button width in pixels</param>
	/// <param name="height">Button height in pixels</param>
	/// <param name="callback">Action to execute when button is clicked</param>
	/// <param name="tooltip">Optional tooltip text displayed on hover</param>
	/// <param name="buttonMode">Optional mode for special update behavior (can be null)</param>
	/// <param name="extraData">Optional data associated with the button (e.g., mercenary profile ID)</param>
	/// <returns>The created GameObject for further configuration</returns>
	public static class ButtonFactory
	{
		public static GameObject CreateButton(
			GameObject parent,
			string buttonId,
			string text,
			Vector2? positionOffset = null,
			float width = 40f,
			float height = 16f,
			System.Action callback = null,
			string tooltip = null,
			object buttonMode = null,
			string extraData = null)
		{
			if (parent == null)
			{
				Debug.LogWarning("[QuasimorphHelloWorld] Parent GameObject is null, cannot create button.");
				return null;
			}

			GameObject buttonGO = new GameObject($"Button_{buttonId}", typeof(RectTransform));
			buttonGO.transform.SetParent(parent.transform, false);

			RectTransform rectTransform = buttonGO.GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.sizeDelta = new Vector2(width, height);
			Vector2 offset = positionOffset ?? Vector2.zero;
			rectTransform.anchoredPosition3D = new Vector3(offset.x, offset.y, 0f);

			// Background image, required for CanvasRenderer + as the Button's click target.
			Image backgroundImage = buttonGO.AddComponent<Image>();
			backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

			Button button = buttonGO.AddComponent<Button>();
			button.targetGraphic = backgroundImage;
			if (callback != null)
			{
				button.onClick.AddListener(() => callback());
			}

			// Label
			GameObject labelGO = new GameObject("Label", typeof(RectTransform));
			labelGO.transform.SetParent(buttonGO.transform, false);
			RectTransform labelRect = labelGO.GetComponent<RectTransform>();
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.sizeDelta = Vector2.zero;
			labelRect.anchoredPosition3D = Vector3.zero;

			Text label = labelGO.AddComponent<Text>();
			label.text = text;
			label.alignment = TextAnchor.MiddleCenter;
			label.fontSize = Mathf.RoundToInt(height * 0.7f);
			label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			label.color = Color.white;
			label.raycastTarget = false;

			ButtonUserData userData = buttonGO.AddComponent<ButtonUserData>();
			userData.Callback = callback;
			userData.Tooltip = tooltip;
			userData.Mode = buttonMode;
			userData.ExtraData = extraData;

			Debug.Log($"[QuasimorphHelloWorld] Created button: {buttonId} at position {offset}");

			return buttonGO;
		}
	}
}