﻿using System.Collections.Generic;

namespace StructureMap.Diagnostics
{
    public class NulloBulletStyle : IBulletStyle
    {
        public void ApplyBullets(IEnumerable<TabbedLine> lines)
        {
            lines.Each(x => x.Bullet = string.Empty);
        }
    }
}