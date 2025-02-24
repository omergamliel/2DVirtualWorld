﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My2DWorldShared.DataEntities
{
    public class ShopItemEntity : BaseEntity
    {
        public int ShopId { get; set; }
        public virtual ShopEntity? Shop { get; set; }


        public int ItemId { get; set; }
        public virtual ItemEntity? Item { get; set; }
    }
}
