﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My2DWorldShared.DataEntities
{
    public class NpcSpeechEntity : BaseEntity
    {
        public int NpcId { get; set; }
        public virtual NpcEntity? Npc { get; set; }
        [MaxLength(90)]
        public string? Speech { get; set; }
    }
}
