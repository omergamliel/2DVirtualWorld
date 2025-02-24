﻿using My2DWorldShared.Enums;
using My2DWorldShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My2DWorldShared.PacketsOut
{
    public class PacketSendInventoryBatch : PacketOutBase
    {
        public InventoryModel? Inventory { get; set; }
        public PacketSendInventoryBatch()
        {
            Id = OutComingPacketId.SendInventoryBatch;
        }
    }
}
