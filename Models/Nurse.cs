using Microsoft.AspNetCore.Identity;

namespace MedicalClinic.Models
{
    public class Nurse
    {
        public int NurseId { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public string Name { get; set; }

        // RELAȚII
        public ICollection<Appointment> Appointments { get; set; }

        // REQ-8: Doctori la care e asignată
        public ICollection<NurseDoctorAssignment> DoctorAssignments { get; set; }
    }
}
