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
    public enum FeedbackType
    {
        INTERACTION_DISLIKE_TYPE_UNSPECIFIED = 0,
        // The content is irrelevant
        INTERACTION_DISLIKE_TYPE_IRRELEVANT = 1,
        // The content is unsafe
        INTERACTION_DISLIKE_TYPE_UNSAFE = 2,
        // The content is untrue
        INTERACTION_DISLIKE_TYPE_UNTRUE = 3,
        // The content uses knowledge incorrectly
        INTERACTION_DISLIKE_TYPE_INCORRECT_USE_KNOWLEDGE = 4,
        // The content contains unexpected action
        INTERACTION_DISLIKE_TYPE_UNEXPECTED_ACTION = 5,
        // The content contains unexpected goal behaviour
        INTERACTION_DISLIKE_TYPE_UNEXPECTED_GOAL_BEHAVIOR = 6,
        // The content contains repetition issue
        INTERACTION_DISLIKE_TYPE_REPETITION = 7
    }
    public enum MicrophoneMode
    {
        UNSPECIFIED,
        OPEN_MIC, // For auto push
        EXPECT_AUDIO_END // For push to talk
    }
    public enum ControlType
    {
        UNKNOWN = 0,
        // Speech activity starts, server should expect DataChunk, TextEvent and
        // EmotionEvent packets after that.
        AUDIO_SESSION_START = 1,
        // Speech activity ended.
        AUDIO_SESSION_END = 2,
        // Indicates that the server has already sent all TextEvent response packets for the given interaction, and there won't be any more. 
        // Other types of packets can still be received by the client after it has received this packet.
        INTERACTION_END = 3,
        // TTS response playback starts on the client.
        TTS_PLAYBACK_START = 4,
        // TTS Response playback ends on the client.
        TTS_PLAYBACK_END = 5,
        // TTS response playback is muted on the client.
        TTS_PLAYBACK_MUTE = 6,
        // TTS response playback is unmuted on the client.
        TTS_PLAYBACK_UNMUTE = 7,
        // Contains warning for client.
        WARNING = 8,
        // Indicates that server is going to close the connection.
        SESSION_END = 9,
        // Start a conversation
        CONVERSATION_START = 10,// [deprecated = true];
        // Update conversation settings. Uses payload_structured type ConversationUpdatePayload
        CONVERSATION_UPDATE = 12,
        // Server message to client with conversation id
        CONVERSATION_STARTED = 13,// [deprecated = true];
        // Conversation events. Contains payload_structured type ConversationEventPayload
        CONVERSATION_EVENT = 14,
        // Contains info about currently loaded scene. For example, scene name, description, loaded agents.
        CURRENT_SCENE_STATUS = 15,
        // Session configuration. Uses payload_structured type SessionConfigurationEvent
        SESSION_CONFIGURATION = 16
    }
    public enum ConversationEventType
    {
        EVICTED,
        STARTED,
        UPDATED,
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
