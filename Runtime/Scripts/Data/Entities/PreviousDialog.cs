/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using Inworld.Packet;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inworld.Entities
{
#region Legacy
    [Serializable]
    public enum PreviousTalker
    {
        UNKNOWN,
        PLAYER,
        CHARACTER
    }
    [Serializable]
    public class PreviousSessionResponse
    {
        public string state;
        public string creationTime;
    }
    [Serializable]
    public class SessionContinuation
    {
        public PreviousDialog previousDialog;
        public string previousState;
    }
    [Serializable]
    public class PreviousDialog
    {
        public PreviousDialogPhrase[] phrases;
    }
    [Serializable]
    public class PreviousDialogPhrase
    {
        public PreviousTalker talker; 
        public string phrase;
    }
    [Serializable]
    public class SessionContinuationContinuationInfo
    {
        public string millisPassed;
    }
#endregion

#region New
    [Serializable]
    public class ContinuationInfo
    {
        string passedTime;
    }
    [Serializable]
    public enum ContinuationType 
    {
        CONTINUATION_TYPE_UNKNOWN = 0,
        CONTINUATION_TYPE_EXTERNALLY_SAVED_STATE = 1,
        CONTINUATION_TYPE_DIALOG_HISTORY = 2
    }
    [Serializable]
    public class HistoryItem
    {
        public string actor;
        public string text;
    }
    [Serializable]
    public class DialogHistory
    {
        public List<HistoryItem> history;
    }
    [Serializable]
    public class Continuation
    {
        public ContinuationInfo continuationInfo;
        // Required
        // Contains type of continuation.
        public ContinuationType continuationType;
        // Dialog that was before starting with existing conversation.
        public DialogHistory dialogHistory;
        // State received from server to use later for session continuation.
        // The state sent in compressed and encrypted format.
        // Client receives it in bytearray format that's why it is not strongly typed.
        // But it is strongly typed on server side and can be deserialized to ExternallySavedState.
        // Client should not modify this state!
        public string externallySavedState;

        public bool IsValid
        {
            get
            {
                switch (continuationType)
                {
                    case ContinuationType.CONTINUATION_TYPE_DIALOG_HISTORY:
                        return dialogHistory?.history?.Count > 0;
                    case ContinuationType.CONTINUATION_TYPE_EXTERNALLY_SAVED_STATE:
                        return !string.IsNullOrEmpty(externallySavedState);
                    default:
                        return false;
                }
            }
        }
        
        public ContinuationPacket ToPacket => new ContinuationPacket
        {
            timestamp = InworldDateTime.UtcNow,
            type = "SESSION_CONTROL",
            packetId = new PacketId(),
            routing = new Routing(),
            sessionControl = new ContinuationEvent
            {
                continuation = this
            }
        };
    }
    [Serializable]
    public class ContinuationEvent
    {
        public Continuation continuation;
    }
    [Serializable]
    public class ContinuationPacket : InworldPacket
    {
        public ContinuationEvent sessionControl;

        public override string ToJson => JsonUtility.ToJson(this);
    }

  #endregion
}
