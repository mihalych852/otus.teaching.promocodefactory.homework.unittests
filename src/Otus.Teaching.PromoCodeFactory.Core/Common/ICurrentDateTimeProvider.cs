using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otus.Teaching.PromoCodeFactory.Core.Common
{
    public interface ICurrentDateTimeProvider
    {
        DateTime CurrentDateTime { get; }
    }
}
