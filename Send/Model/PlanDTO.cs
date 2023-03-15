using System;
namespace ServiceWorker
{
	public class PlanDTO
	{
        public string KundeNavn { get; set; }
        public DateTime StartTidspunkt { get; set; }
        public string StartSted { get; set; }
        public string SlutSted { get; set; }

        public PlanDTO(string kundenavn, DateTime starttidspunkt, string startsted, string slutsted)
        {
            this.KundeNavn = kundenavn;
            this.StartTidspunkt = starttidspunkt;
            this.StartSted = startsted;
            this.SlutSted = slutsted;
        }

        public PlanDTO()
		{

		}
	}
}

