using UnityEngine;

namespace ConsentProximityFramework.Runtime.Networking
{
    ///<summary>
    /// manages the consent flow state for an interaction
    /// this class acts as the central authority for consent logic
    /// </summary>
    public class ConsentFlowManager : MonoBehaviour //Scrip attached to GameObject
    {
        //current consent state
        [SerializeField]//show pv variable in inspector(only in unity - safe visibility)
        private ConsentState currentState = ConsentState.Idle;
         ///<summary>
         /// Gets the current consent state
         /// </summary>
         public ConsentState CurrentState
        {
            get { return currentState; }
        }

        ///<summary>
        /// Sets a new consent state
        /// all state changes should go through this method
        /// </summary
        public void SetState(ConsentState newState)
        {
            currentState = newState;
            Debug.Log($"Consent state changed to: {currentState}");
        }

        ///<summary>
        /// called when another user leaves proximity range
        /// </suumary>
    public void EnterProximity()
        {
            if(currentState == ConsentState.Idle)
            {
                SetState(ConsentState.InRange);
            }
        }

        ///<summary>
        /// Called when another user leaves proximity range
        /// </summary>
        public void ExitProximity()
        {
            if(currentState == ConsentState.InRange)
            {
                SetState(ConsentState.Idle);
            }
        }

    }
}