using EditorGizmos;
using Expansions.Missions.Editor;
using KSP.UI;
using KSP.UI.TooltipTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TweakScale
{
	// this is a component that gets added to the instance of EditorToolsUI and interacts with it in order to provide an additional "scale" tool mode in the editor
	internal class ConstructionModeScale : MonoBehaviour
	{
		Toggle scaleButton;
		KeyBinding keyBinding;
		EditorToolsUI editorToolsUI;

		KFSMEvent on_goToModeScale;
		KFSMEvent on_scaleSelect;
		KFSMEvent on_scaleDeselect;
		KFSMEvent on_scaleReset;

		KFSMState st_scale_select;
		KFSMState st_scale_tweak;

		const ConstructionMode scaleConstructionMode = (ConstructionMode)4;

		void Start()
		{
			editorToolsUI = GetComponent<EditorToolsUI>();

			keyBinding = new KeyBinding(TweakScaleEditorLogic.Instance.ScaleModeKey, ControlTypes.EDITOR_GIZMO_TOOLS | ControlTypes.KEYBOARDINPUT);

			CreateToolButton();

			GameEvents.onEditorConstructionModeChange.Remove(EditorLogic.fetch.onConstructionModeChanged);
			GameEvents.onEditorConstructionModeChange.Add(onConstructionModeChanged);

			PatchEditorFSM();
		}

		void CreateToolButton()
		{
			scaleButton = GameObject.Instantiate(editorToolsUI.rootButton);
			var buttonTransform = scaleButton.transform as RectTransform;
			var buttonPosition = buttonTransform.anchoredPosition;
			buttonPosition.x += (editorToolsUI.rootButton.transform as RectTransform).anchoredPosition.x - (editorToolsUI.rotateButton.transform as RectTransform).anchoredPosition.x;
			buttonTransform.SetParent(editorToolsUI.rootButton.transform.parent, false);
			buttonTransform.anchoredPosition = buttonPosition;

			scaleButton.gameObject.name = "scaleButton";
			scaleButton.GetComponent<TooltipController_Text>().SetText("Tool: Scale");

			Texture2D offIconTexture = GameDatabase.Instance.GetTexture("TweakScale/icons/scaleTool_off", false);
			Texture2D onIconTexture = GameDatabase.Instance.GetTexture("TweakScale/icons/scaleTool_on", false);
			var oldSprite = scaleButton.image.sprite;
			scaleButton.image.sprite = Sprite.Create(offIconTexture, oldSprite.rect, oldSprite.pivot);

			(scaleButton.graphic as Image).sprite = Sprite.Create(onIconTexture, oldSprite.rect, oldSprite.pivot);

			scaleButton.onValueChanged.AddListener(onScaleButtonInput);
		}

		Part selectedPart
		{
			get => EditorLogic.fetch.selectedPart;
			set { EditorLogic.fetch.selectedPart = value; }
		}

		GizmoOffset gizmoScale;

		static KFSMCallback Combine(params KFSMCallback[] callbacks)
		{
			return (KFSMCallback)Delegate.Combine(callbacks);
		}

		void PatchEditorFSM()
		{
			KerbalFSM fsm = EditorLogic.fetch.fsm;
			int layerMask = EditorLogic.fetch.layerMask | 4 | 0x200000;

			// add states

			st_scale_select = new KFSMState("st_scale_select")
			{
				OnUpdate = Combine(
					EditorLogic.fetch.UndoRedoInputUpdate,
					EditorLogic.fetch.snapInputUpdate,
					EditorLogic.fetch.partSearchUpdate)
			};
			fsm.AddState(st_scale_select);

			st_scale_tweak = new KFSMState("st_scale_tweak")
			{
				OnEnter = delegate
				{
					selectedPart.onEditorStartTweak();
					Transform referenceTransform = selectedPart.GetReferenceTransform();
					EditorLogic.fetch.symUpdateMode = selectedPart.symmetryCounterparts.Count;
					if (EditorLogic.fetch.ship.Contains(selectedPart))
					{
						EditorLogic.fetch.symUpdateParent = selectedPart.parent;
						EditorLogic.fetch.symUpdateAttachNode = selectedPart.FindAttachNodeByPart(EditorLogic.fetch.symUpdateParent);
					}
					else
					{
						EditorLogic.fetch.symUpdateParent = EditorLogic.fetch.attachment.potentialParent;
						EditorLogic.fetch.symUpdateAttachNode = EditorLogic.fetch.attachment.callerPartNode;
					}

					if (EditorLogic.fetch.symUpdateAttachNode != null)
					{
						EditorLogic.fetch.gizmoPivot = referenceTransform.TransformPoint(EditorLogic.fetch.symUpdateAttachNode.position);
					}
					else
					{
						EditorLogic.fetch.gizmoPivot = referenceTransform.transform.position;
					}

					gizmoScale = GizmoOffset.Attach(referenceTransform, selectedPart.initRotation, null, onScaleGizmoUpdated, EditorLogic.fetch.editorCamera);
					gizmoScale.transform.position = gizmoScale.trfPos0 = EditorLogic.fetch.gizmoPivot;
					gizmoScale.useGrid = true; // HACK: we use this to test whether we need to rebind the handle events in partscaleInputUpdate
					GameEvents.onEditorSnapModeChange.Remove(gizmoScale.onEditorSnapChanged);
					EditorLogic.fetch.audioSource.PlayOneShot(EditorLogic.fetch.tweakGrabClip);
				},
				OnUpdate = Combine(
					partscaleInputUpdate,
					EditorLogic.fetch.UndoRedoInputUpdate,
					EditorLogic.fetch.snapInputUpdate,
					EditorLogic.fetch.deleteInputUpdate,
					EditorLogic.fetch.partSearchUpdate),
				OnLeave = (KFSMState to) =>
				{
					gizmoScale.Detach();
					EditorLogic.fetch.symUpdateMode = 0;
					EditorLogic.fetch.symUpdateParent = null;
					EditorLogic.fetch.symUpdateAttachNode = null;
					if (to != EditorLogic.fetch.st_offset_tweak && to != EditorLogic.fetch.st_rotate_tweak && selectedPart != null)
					{
						selectedPart.onEditorEndTweak();
						if (to == EditorLogic.fetch.st_idle)
						{
							selectedPart = null;
						}
					}

					EditorLogic.fetch.audioSource.PlayOneShot(EditorLogic.fetch.tweakReleaseClip);
				}
			};
			fsm.AddState(st_scale_tweak);

			// add events

			on_goToModeScale = new KFSMEvent("on_goToModeScale")
			{
				updateMode = KFSMUpdateMode.MANUAL_TRIGGER,
				OnEvent = delegate 
				{
					if (EditorLogic.fetch.selectedPart == null)
					{
						ScreenMessages.PostScreenMessage("Select a part to Scale", EditorLogic.fetch.modeMsg);
						on_goToModeScale.GoToStateOnEvent = st_scale_select;
					}
					else if (!EditorLogic.fetch.ship.Contains(EditorLogic.fetch.selectedPart))
					{
						on_goToModeScale.GoToStateOnEvent = EditorLogic.fetch.st_place;
						EditorLogic.fetch.on_partPicked.OnEvent();
					}
					else
					{
						on_goToModeScale.GoToStateOnEvent = st_scale_tweak;
					}
				}
			};
			fsm.AddEvent(on_goToModeScale, EditorLogic.fetch.st_idle, EditorLogic.fetch.st_offset_select, EditorLogic.fetch.st_offset_tweak, EditorLogic.fetch.st_rotate_select, EditorLogic.fetch.st_rotate_tweak, EditorLogic.fetch.st_root_unselected, EditorLogic.fetch.st_root_select);

			on_scaleSelect = new KFSMEvent("on_scaleSelect")
			{
				updateMode = KFSMUpdateMode.UPDATE,
				OnCheckCondition = delegate
				{
					if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
					{
						selectedPart = EditorLogic.fetch.pickPart(layerMask, Input.GetKey(KeyCode.LeftShift), pickRootIfFrozen: false);
						if (EditorLogic.fetch.selectedPart != null)
						{
							if (!EditorLogic.fetch.ship.Contains(selectedPart))
							{
								on_scaleSelect.GoToStateOnEvent = EditorLogic.fetch.st_place;
								EditorLogic.fetch.on_partPicked.OnEvent();
								return false;
							}

							on_scaleSelect.GoToStateOnEvent = st_scale_tweak;
							return true;
						}
					}

					return false;
				}
			};
			fsm.AddEvent(on_scaleSelect, st_scale_select);

			on_scaleDeselect = new KFSMEvent("on_scaleDeselect")
			{
				GoToStateOnEvent = st_scale_select,
				updateMode = KFSMUpdateMode.UPDATE,
				OnCheckCondition = delegate
				{
					if (Mouse.Left.GetButtonDown() && !Mouse.Left.WasDragging() && !gizmoScale.GetMouseOverGizmo && !EventSystem.current.IsPointerOverGameObject())
					{
						Part pickedPart = EditorLogic.fetch.pickPart(layerMask, Input.GetKey(KeyCode.LeftShift), pickRootIfFrozen: false);
						if (pickedPart == null)
						{
							selectedPart.onEditorEndTweak();
							selectedPart.gameObject.SetLayerRecursive(0, filterTranslucent: true, 2097152);
							selectedPart = null;
							return true;
						}

						if (EditorGeometryUtil.GetPixelDistance(gizmoScale.transform.position, Input.mousePosition, EditorLogic.fetch.editorCamera) > 75f)
						{
							selectedPart.onEditorEndTweak();
							selectedPart.gameObject.SetLayerRecursive(0, filterTranslucent: true, 2097152);
							selectedPart = pickedPart;
							return true;
						}
					}

					return false;
				}
			};
			fsm.AddEvent(on_scaleDeselect, st_scale_tweak);

			on_scaleReset = new KFSMEvent("on_scaleReset")
			{
				GoToStateOnEvent = st_scale_tweak,
				updateMode = KFSMUpdateMode.MANUAL_TRIGGER
			};
			fsm.AddEvent(on_scaleReset, st_scale_tweak);

			EditorLogic.fetch.on_undoRedo.OnEvent += delegate
			{
				if (fsm.currentState == st_scale_tweak)
				{
					EditorLogic.fetch.on_undoRedo.GoToStateOnEvent = st_scale_select;
				}
			};

			// add existing events to our new states
			fsm.AddEvent(EditorLogic.fetch.on_partDeleted, st_scale_tweak);
			fsm.AddEvent(EditorLogic.fetch.on_goToModeRotate, st_scale_select, st_scale_tweak);
			fsm.AddEvent(EditorLogic.fetch.on_goToModePlace, st_scale_select, st_scale_tweak);
			fsm.AddEvent(EditorLogic.fetch.on_goToModeOffset, st_scale_select, st_scale_tweak);
			fsm.AddEvent(EditorLogic.fetch.on_goToModeRoot, st_scale_select, st_scale_tweak);
			fsm.AddEvent(EditorLogic.fetch.on_undoRedo, st_scale_tweak);
			fsm.AddEvent(EditorLogic.fetch.on_podDeleted, st_scale_select, st_scale_tweak);
			fsm.AddEvent(EditorLogic.fetch.on_partCreated, st_scale_select, st_scale_tweak);
			fsm.AddEvent(EditorLogic.fetch.on_partOverInventoryPAW, st_scale_select, st_scale_tweak);
			fsm.AddEvent(EditorLogic.fetch.on_newShip, st_scale_select, st_scale_tweak);
			fsm.AddEvent(EditorLogic.fetch.on_shipLoaded, st_scale_select, st_scale_tweak);
		}

		float previousScaleFactor = 1.0f;
		private void onScaleGizmoHandleDragStart(GizmoOffsetHandle handle, Vector3 axis)
		{
			var tweakScaleModule = selectedPart.FindModuleImplementing<TweakScale>();
			previousScaleFactor = tweakScaleModule.currentScaleFactor;
		}

		static ScaleFactorSnapMode GetScaleSnapMode()
		{
			ScaleFactorSnapMode snapMode = ScaleFactorSnapMode.None;

			if (GameSettings.VAB_USE_ANGLE_SNAP)
			{
				snapMode = GameSettings.Editor_fineTweak.GetKey() ? ScaleFactorSnapMode.FineSteps : ScaleFactorSnapMode.CoarseSteps;
			}

			return snapMode;
		}

		private void onScaleGizmoHandleDrag(GizmoOffsetHandle handle, Vector3 axis, float amount)
		{
			var tweakScaleModule = selectedPart.FindModuleImplementing<TweakScale>();
			float newScaleFactor = previousScaleFactor + amount;
			tweakScaleModule.SetScaleFactor(newScaleFactor, GetScaleSnapMode());
		}

		private void onScaleGizmoUpdated(Vector3 arg1)
		{
			if (EditorLogic.fetch.ship.Contains(selectedPart))
			{
				EditorLogic.fetch.SetBackup();
			}

			GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartOffset, selectedPart);
		}

		private void partscaleInputUpdate()
		{
			// hook up events if necessary - super hacky!
			// GizmoOffset doesn't set up its handle events until Start, so we need to wait until after that to overwrite them
			if (gizmoScale.useGrid && gizmoScale.handles[0].onDrag != null)
			{
				foreach (var handle in gizmoScale.handles)
				{
					handle.onDragStart += onScaleGizmoHandleDragStart;
					handle.onDrag = onScaleGizmoHandleDrag;
				}
				gizmoScale.useGrid = false;
			}

			// TODO: should this be cached..?
			var tweakScaleModule = selectedPart.FindModuleImplementing<TweakScale>();
			var centerMessage = $"Scale: {tweakScaleModule.GetScaleString()}";

			var keybindMessage = 
				$"[{GameSettings.Editor_toggleAngleSnap.name}] Toggle Snap\n" +
				$"[{GameSettings.Editor_fineTweak.name}] Fine control\n" +
				$"[{TweakScaleEditorLogic.Instance.IncreaseScaleKey}/{TweakScaleEditorLogic.Instance.DecreaseScaleKey}] Inc/Dec Scale\n" +
				$"[{TweakScaleEditorLogic.Instance.NextScaleIntervalKey}/{TweakScaleEditorLogic.Instance.PrevScaleIntervalKey}] Next/Prev Scale\n" +
				$"[{GameSettings.Editor_resetRotation.name}] Reset Scale\n" +
				  TweakScaleEditorLogic.Instance.ScaleChildren.GetKeybindPrompt();

			ScreenMessages.PostScreenMessage(centerMessage, 0f, ScreenMessageStyle.LOWER_CENTER);
			ScreenMessages.PostScreenMessage(keybindMessage, 0f, ScreenMessageStyle.UPPER_CENTER);

			KerbalFSM fsm = EditorLogic.fetch.fsm;

			if (InputLockManager.IsUnlocked(ControlTypes.EDITOR_GIZMO_TOOLS))
			{
				int incrementDirection = Input.GetKeyDown(TweakScaleEditorLogic.Instance.IncreaseScaleKey).ToInt() - Input.GetKeyDown(TweakScaleEditorLogic.Instance.DecreaseScaleKey).ToInt();
				if (incrementDirection != 0)
				{
					tweakScaleModule.IncrementScaleFactor(incrementDirection, GetScaleSnapMode());
				}

				int jumpDirection = Input.GetKeyDown(TweakScaleEditorLogic.Instance.NextScaleIntervalKey).ToInt() - Input.GetKeyDown(TweakScaleEditorLogic.Instance.PrevScaleIntervalKey).ToInt();
				if (jumpDirection != 0)
				{
					tweakScaleModule.JumpScaleFactor(jumpDirection);
				}

				// did we press the reset button?
				if (GameSettings.Editor_resetRotation.GetKeyDown() && selectedPart.transform == selectedPart.GetReferenceTransform())
				{
					tweakScaleModule.SetScaleFactor(1.0f);

					if (fsm.CurrentState == st_scale_tweak)
					{
						fsm.RunEvent(on_scaleReset);
					}

					// TODO: do we need to emit a construction event?  What kind?  Where?
					//GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartOffset, selectedPart);
				}
			}

			foreach (var part in selectedPart.symmetryCounterparts)
			{
				var symTweakScaleModule = part.FindModuleImplementing<TweakScale>();
				symTweakScaleModule.SetScaleFactor(tweakScaleModule.currentScaleFactor);
			}
		}

		private void onConstructionModeChanged(ConstructionMode mode)
		{
			if (mode == EditorLogic.fetch.constructionMode) return;

			if (mode == scaleConstructionMode)
			{
				EditorLogic.fetch.coordSpaceBtn.gameObject.SetActive(value: false);
				EditorLogic.fetch.radialSymmetryBtn.gameObject.SetActive(value: false);

				EditorLogic.fetch.fsm.RunEvent(on_goToModeScale);

				EditorLogic.fetch.constructionMode = mode;
			}
			else
			{
				EditorLogic.fetch.onConstructionModeChanged(mode);
			}
		}

		void OnDestroy()
		{
			scaleButton.onValueChanged.RemoveListener(onScaleButtonInput);
			GameEvents.onEditorConstructionModeChange.Remove(onConstructionModeChanged);
		}

		void Update()
		{
			// TODO: check input locks, ets
			if (keyBinding.GetKeyDown())
			{
				SetMode(scaleConstructionMode, true);
			}
		}

		private void onScaleButtonInput(bool b)
		{
			if (b && scaleButton.interactable)
			{
				SetMode(scaleConstructionMode, false);
			}
		}

		public void SetMode(ConstructionMode mode, bool updateUI)
		{
			editorToolsUI.SetMode(mode, updateUI);

			if (editorToolsUI.constructionMode == scaleConstructionMode && updateUI)
			{
				scaleButton.isOn = true;
			}
		}
	}
}
