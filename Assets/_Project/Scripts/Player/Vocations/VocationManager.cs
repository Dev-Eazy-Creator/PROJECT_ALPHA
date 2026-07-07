// Tracks the active vocation and broadcasts changes. Other systems subscribe — never poll.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAlpha
{
    public class VocationManager : MonoBehaviour
    {
        [SerializeField] private List<VocationData> allVocations = new List<VocationData>();
        [SerializeField] private VocationData startingVocation;

        public IReadOnlyList<VocationData> AllVocations => allVocations;
        public VocationData CurrentVocation { get; private set; }

        public event Action<VocationData> OnVocationChanged;

        private void Awake()
        {
            // Set the starting vocation without firing the event on init.
            CurrentVocation = startingVocation;
        }

        public void SetVocation(VocationType type)
        {
            VocationData match = null;
            for (int i = 0; i < allVocations.Count; i++)
            {
                if (allVocations[i] != null && allVocations[i].Vocation == type)
                {
                    match = allVocations[i];
                    break;
                }
            }

            if (match == null)
            {
                Debug.LogWarning($"[VocationManager] No VocationData found for {type}. Ignoring SetVocation.");
                return;
            }

            if (match == CurrentVocation)
            {
                return;
            }

            CurrentVocation = match;
            OnVocationChanged?.Invoke(CurrentVocation);
        }
    }
}
