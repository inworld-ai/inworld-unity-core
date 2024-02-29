/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/

namespace Inworld
{
    public enum InworldConnectionStatus
    {
        Idle, // Initial state
        Initializing, // Used at getting runtime token.
        Initialized, // Getting runtime Token Completed. 
        Connecting, // Start Session with Inworld Server by runtime token.
        Connected, // Controller is connected to World-Engine and ready to work.
        Exhausted, // Received when user is running out of quota.
        Error // Some error occured.
    }

    public enum InworldMessage
    {
        None,
        GoalEnable,
        GoalDisable,
        GoalComplete,
        RelationUpdate,
        ConversationNextTurn,
        Error,
        Critical,
        GoAway,
        IncompleteInteraction
    }
    public enum ControlType
    {
        UNKNOWN,
        AUDIO_SESSION_START,
        AUDIO_SESSION_END,
        INTERACTION_END,
        WARNING,
    }
    public enum SourceType
    {
        NONE,
        AGENT,
        PLAYER,
        WORLD
    }
    public enum PacketType
    {
        UNKNOWN,
        TEXT,
        CONTROL,
        AUDIO,
        GESTURE,
        CUSTOM,
        CANCEL_RESPONSE,
        EMOTION,
        ACTION,
        SESSION_RESPONSE
    }

    public enum MicSampleMode
    {
        NO_MIC,
        NO_FILTER,
        PUSH_TO_TALK,
        AEC,
        TURN_BASED
    }
}
