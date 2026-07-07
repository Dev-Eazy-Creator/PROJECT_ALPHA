// Tracks the active job and broadcasts changes. Other systems subscribe — never poll.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectAlpha
{
    // Runs first so CurrentJob is set in Awake before CharacterStats (-90) builds its AttributeSet.
    [DefaultExecutionOrder(-100)]
    public class JobManager : MonoBehaviour
    {
        [FormerlySerializedAs("allVocations")]
        [SerializeField] private List<JobData> allJobs = new List<JobData>();
        [FormerlySerializedAs("startingVocation")]
        [SerializeField] private JobData startingJob;

        public IReadOnlyList<JobData> AllJobs => allJobs;
        public JobData CurrentJob { get; private set; }

        public event Action<JobData> OnJobChanged;

        private void Awake()
        {
            // Set the starting job without firing the event on init.
            CurrentJob = startingJob;
        }

        public void SetJob(JobType type)
        {
            JobData match = null;
            for (int i = 0; i < allJobs.Count; i++)
            {
                if (allJobs[i] != null && allJobs[i].Job == type)
                {
                    match = allJobs[i];
                    break;
                }
            }

            if (match == null)
            {
                Debug.LogWarning($"[JobManager] No JobData found for {type}. Ignoring SetJob.");
                return;
            }

            if (match == CurrentJob)
            {
                return;
            }

            CurrentJob = match;
            OnJobChanged?.Invoke(CurrentJob);
        }
    }
}
