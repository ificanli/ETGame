using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public class ECAPointViewMarker : MonoBehaviour
    {
        [Header("点位绑定")]
        public string PointId;
        public Animator TargetAnimator;

        [Header("开关状态")]
        public List<int> OpenStates = new() { 1 };

        [Header("Animator参数")]
        public bool UseOpenBool = true;
        public string OpenBoolParam = "IsOpen";
        public bool UseStateInt = false;
        public string StateIntParam = "State";
        public bool UseOpenCloseTrigger = false;
        public string OpenTriggerParam = "Open";
        public string CloseTriggerParam = "Close";

        private void Reset()
        {
            this.SyncBinding();
        }

        private void OnValidate()
        {
            this.SyncBinding();
        }

        private void SyncBinding()
        {
            ECAPointMarker pointMarker = this.GetComponent<ECAPointMarker>();
            if (pointMarker == null)
            {
                pointMarker = this.GetComponentInParent<ECAPointMarker>();
            }

            if (pointMarker != null && !string.IsNullOrWhiteSpace(pointMarker.ConfigId))
            {
                this.PointId = pointMarker.ConfigId;
            }

            if (this.TargetAnimator == null)
            {
                this.TargetAnimator = this.GetComponent<Animator>();
            }

            if (this.TargetAnimator == null)
            {
                this.TargetAnimator = this.GetComponentInChildren<Animator>(true);
            }

            this.OpenStates ??= new List<int>();
            this.ApplyDefaultOpenStates(pointMarker);
        }

        private void ApplyDefaultOpenStates(ECAPointMarker pointMarker)
        {
            if (pointMarker == null)
            {
                return;
            }

            if (this.OpenStates.Count > 1)
            {
                return;
            }

            int defaultState = this.OpenStates.Count == 1 ? this.OpenStates[0] : int.MinValue;
            if (defaultState != int.MinValue && defaultState != ContainerState.Opened && defaultState != ECADoorState.Opened)
            {
                return;
            }

            this.OpenStates.Clear();
            switch (pointMarker.Type)
            {
                case ECAPointType.Door:
                case ECAPointType.KeyDoor:
                    this.OpenStates.Add(ECADoorState.Opened);
                    break;
                default:
                    this.OpenStates.Add(ContainerState.Opened);
                    break;
            }
        }
    }
}
