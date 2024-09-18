/*************************************************************************************************
 * Copyright 2022-2024 Theai, Inc. dba Inworld AI
 *
 * Use of this source code is governed by the Inworld.ai Software Development Kit License Agreement
 * that can be found in the LICENSE.md file or at https://www.inworld.ai/sdk-license
 *************************************************************************************************/
using System;
using System.Collections.Generic;

namespace Inworld.Packet
{
    [Serializable][Obsolete]
    public class GestureEvent
    {
        public string type;
        public string playback;
    }
    [Serializable][Obsolete]
    public class GesturePacket : InworldPacket
    {
        public GestureEvent gesture;
        
        public GesturePacket()
        {
            gesture = new GestureEvent();
        }
        public GesturePacket(InworldPacket rhs, GestureEvent evt) : base(rhs)
        {
            gesture = evt;
        }
    }
    [Obsolete]
    public class LoadScenePacket : InworldPacket
    {
        public LoadSceneEvent mutation;

        public LoadScenePacket(string sceneFullName)
        {
            timestamp = InworldDateTime.UtcNow;
            packetId = new PacketId();
            routing = new Routing("WORLD");
            mutation = new LoadSceneEvent
            {
                loadScene = new LoadSceneRequest
                {
                    name = sceneFullName
                }
            };
        }
    }
    [Obsolete]
    public class LoadCharactersPacket : InworldPacket
    {
        public LoadCharactersEvent mutation;

        public LoadCharactersPacket(List<string> characterFullName)
        {
            timestamp = InworldDateTime.UtcNow;
            packetId = new PacketId();
            routing = new Routing();
            mutation = new LoadCharactersEvent
            {
                loadCharacters = new LoadCharactersRequest(characterFullName)
            };
        }
    }
}
