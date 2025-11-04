using System;
using System.Collections.Generic;

namespace EPIFlow.Biometria.Agent.Models
{
    public class AgentTaskMessage
    {
        public Guid TaskId { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeRegistrationNumber { get; set; } = string.Empty;
        public int Finger { get; set; }
        public string FingerName { get; set; } = string.Empty;
        public Dictionary<string, object> Payload { get; set; } = new Dictionary<string, object>();

        public string ResolveEmployeeIdentifier()
        {
            object colaboradorId;
            if (Payload.TryGetValue("colaboradorId", out colaboradorId))
            {
                var colaborador = colaboradorId as string;
                if (!string.IsNullOrWhiteSpace(colaborador))
                {
                    return colaborador;
                }
            }

            if (!string.IsNullOrWhiteSpace(EmployeeRegistrationNumber))
            {
                return EmployeeRegistrationNumber;
            }

            return EmployeeId.ToString();
        }

        public string ResolveOperation()
        {
            if (Payload.TryGetValue("operation", out var operationValue))
            {
                var operation = operationValue?.ToString();
                if (!string.IsNullOrWhiteSpace(operation))
                {
                    return operation;
                }
            }

            return "enroll";
        }

        public string ResolveTemplateBase64()
        {
            if (Payload.TryGetValue("templateBase64", out var templateValue))
            {
                var template = templateValue?.ToString();
                if (!string.IsNullOrWhiteSpace(template))
                {
                    return template;
                }
            }

            return null;
        }
    }
}
