using System;

namespace Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement
{
    public class PartnerPromoCodeLimit
    {
        private DateTime? _cancelDate;

        public Guid Id { get; set; }

        public Guid PartnerId { get; set; }

        public virtual Partner Partner { get; set; }
        
        public DateTime CreateDate { get; set; }

        public DateTime? CancelDate
        {
            get
            {
                return _cancelDate?.Date;
            }
            set
            {
                _cancelDate = value;
            }
        }

        public DateTime EndDate { get; set; }

        public int Limit { get; set; }
    }
}