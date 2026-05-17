namespace MedicalClinic.Models
{
    /// <summary>REQ-8: Definire relație asistentă ↔ doctor</summary>
    public class NurseDoctorAssignment
    {
        public int NurseDoctorAssignmentId { get; set; }

        public int NurseId { get; set; }
        public Nurse Nurse { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }
    }
}
