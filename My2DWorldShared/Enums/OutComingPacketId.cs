﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My2DWorldShared.Enums
{
    public enum OutComingPacketId
    {
        ValidateAuthentication,
        PushUserInformation,
        ChangeMap,
        UpdatePosition,
        PlayerJoinedRoom,
        PlayerExitRoom,
        PlayerSpeech,
        PlayerGameLoad,
        SendInventoryBatch,
        PlayerEquipItem,
        PlayerUnequipItem,
        SendShopLoadBatch
    }
}
