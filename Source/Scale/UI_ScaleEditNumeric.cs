using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TweakScale
{
	internal class UI_ScaleEditNumeric : UI_ScaleEdit
	{
		private new const string UIControlName = "ScaleEditNumeric";
	}

	[UI_ScaleEditNumeric]
	class UIPartActionScaleEditNumeric : UIPartActionScaleEdit
	{
		public GameObject numericContainer;
		public TextMeshProUGUI fieldNameNumeric;
		public TMP_InputField inputField;

		public override void Setup(UIPartActionWindow window, Part part, PartModule partModule, UI_Scene scene, UI_Control control, BaseField field)
		{
			base.Setup(window, part, partModule, scene, control, field);

			// UIPartActionFloatRange.Setup:
			inputField.onEndEdit.AddListener(OnFieldInput);
			inputField.onSelect.AddListener(AddInputFieldLock);
			GameEvents.onPartActionNumericSlider.Add(ToggleNumericSlider);
			ToggleNumericSlider(GameSettings.PAW_NUMERIC_SLIDERS);
			window.usingNumericValue = true;

			// other
			fieldNameNumeric.text = string.Format("{0} ({1})", field.guiName, field.guiUnits);
		}

		private void OnDestroy()
		{
			GameEvents.onPartActionNumericSlider.Remove(ToggleNumericSlider);
		}

		private void ToggleNumericSlider(bool numeric)
		{
			slider.gameObject.SetActive(!numeric);
			inc.gameObject.SetActive(!numeric);
			dec.gameObject.SetActive(!numeric);

			numericContainer.SetActive(numeric);
			fieldNameNumeric.gameObject.SetActive(numeric);
			if (inputField.gameObject.activeInHierarchy)
			{
				inputField.text = GetFieldValue().ToString();
			}
		}

		private void OnFieldInput(string input)
		{
			if (float.TryParse(input, out float value))
			{
				value = Mathf.Clamp(value, scaleControl.intervals.First(), scaleControl.intervals.Last());
				SetFieldValue(value);
				UpdateItem();
			}
			RemoveInputfieldLock();
		}

		public override void UpdateItem()
		{
			base.UpdateItem();

			float value = GetFieldValue();
			if (!inputField.isFocused)
			{
				inputField.text = value.ToString();
			}

			// don't change interval unless we have to
			if (value < scaleControl.intervals[intervalIndex] || value > scaleControl.intervals[intervalIndex + 1])
			{
				intervalIndex = FindInterval(value);
			}

			// note: direct access to private versions of these members because we don't want to call value-changed events
			slider.m_MinValue = scaleControl.intervals[intervalIndex];
			slider.m_MaxValue = scaleControl.intervals[intervalIndex + 1];
			slider.SetValueWithoutNotify(value);

			UpdateDisplay(value, null);
		}

		private void InitializeAsPrefab()
		{
			// copy the numeric stuff from the UIPartActionFloatRange prefab
			var floatRangePrefab = UIPartActionController.Instance.fieldPrefabs.FirstOrDefault(p => p.GetType() == typeof(UIPartActionFloatRange)) as UIPartActionFloatRange;

			// copy the numeric parts from the FloatRange prefab
			// note this gameobject is named "InputFieldHolder"
			numericContainer = GameObject.Instantiate(floatRangePrefab.numericContainer);
			numericContainer.transform.SetParent(transform, false);

			// fetch the components we need out of the copy
			fieldNameNumeric = numericContainer.transform.Find("NameNumeric").GetComponent<TextMeshProUGUI>();
			inputField = numericContainer.transform.Find("TextMeshPro - InputField").GetComponent<TMP_InputField>();
		}

		internal static UIPartActionScaleEditNumeric CreatePrefab()
		{
			// copy the stock ScaleEdit prefab
			var scaleEditComponentPrefab = UIPartActionController.Instance.fieldPrefabs.FirstOrDefault(p => p.GetType() == typeof(UIPartActionScaleEdit));
			var sourceScaleEditComponent = GameObject.Instantiate(scaleEditComponentPrefab); // note this clones the whole gameobject

			// add a ScaleEditNumeric, copy the data from the ScaleEdit, and destroy the ScaleEdit
			var scaleEditNumericComponent = sourceScaleEditComponent.gameObject.AddComponent<UIPartActionScaleEditNumeric>();
			Tools.CopyComponentData(sourceScaleEditComponent, scaleEditNumericComponent);
			Component.Destroy(sourceScaleEditComponent.GetComponent<UIPartActionScaleEdit>());

			// initialize the new parts of the ScaleEditNumeric
			scaleEditNumericComponent.InitializeAsPrefab();
			
			return scaleEditNumericComponent;
		}
	}
}
