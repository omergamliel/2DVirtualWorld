﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My2DWorldShared.DataEntities
{
    public class MapEntity : BaseEntity
    {
        [MaxLength(64)]
        public string? Name { get; set; }
        [MaxLength(256)]
        public string? FilePath { get; set; }
        public float SpawnX { get; set; }
        public float SpawnY { get; set; }

        public virtual ICollection<MapExitEntity>? Exits { get; set; }
        public virtual ICollection<MapNpcEntity>? Npcs { get; set; }
    }
}
