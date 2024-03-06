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
    public enum ErrorType
    {
        UNDEFINED = -2,
        CLIENT_ERROR = -1,
        SESSION_TOKEN_EXPIRED = 0,
        // Session token is completely invalid
        SESSION_TOKEN_INVALID = 1,
        // Session's resources are temporarily exhausted
        SESSION_RESOURCES_EXHAUSTED = 2,
        // Billing tokens are exhausted -- client should buy more time or wait till end of billing period
        BILLING_TOKENS_EXHAUSTED = 3,
        // Developer account is completely disabled, either due to a ToS violation or for some other reason
        ACCOUNT_DISABLED = 4,
        // Session is invalid due to missing agents or some other reason
        SESSION_INVALID = 5,
        // Resource id is invalid or otherwise could not be found
        RESOURCE_NOT_FOUND = 6,
        // Safety policies have been violated
        SAFETY_VIOLATION = 7
    }
    public enum ReconnectionType
    {
        UNDEFINED = 0,
        // Client should not try to reconnect
        NO_RETRY = 1,
        // Client can try to reconnect immediately
        IMMEDIATE = 2,
        // Client can try to reconnect after given period, specified in InworldStatus.reconnect_time
        TIMEOUT = 3
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
