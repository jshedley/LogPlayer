﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSCloud.LogPlayer.Types
{
    public interface IChangeLogTracked<T>
        where T:struct
    {
        T? ObjectId { get; set; }
    }
}
