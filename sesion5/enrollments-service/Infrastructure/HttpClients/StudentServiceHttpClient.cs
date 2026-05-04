using Enrollments.Domain.Interfaces;
using System.Text.Json;

namespace Enrollments.Infrastructure.HttpClients;

/// <summary>
/// Adaptador de salida: consulta el servicio de estudiantes via HTTP.
/// Implementa IStudentServiceClient (puerto de dominio).
/// El dominio solo conoce la interfaz, no la implementación HTTP.
/// </summary>
public sealed class StudentServiceHttpClient : IStudentServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<StudentServiceHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StudentServiceHttpClient(HttpClient http, ILogger<StudentServiceHttpClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<StudentInfo?> GetStudentAsync(Guid studentId)
    {
        try
        {
            // Usa el endpoint interno /api/internal/students/{id} que es AllowAnonymous
            // — no requiere JWT, solo accesible dentro de la red Docker
            var response = await _http.GetAsync($"/api/internal/students/{studentId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var json    = await response.Content.ReadAsStringAsync();
            var student = JsonSerializer.Deserialize<StudentApiResponse>(json, JsonOpts);

            return student is null ? null : new StudentInfo(
                student.Id,
                $"{student.FirstName} {student.LastName}",
                student.Email,
                student.EnrollmentNumber,
                student.IsActive);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "[HTTP] Error consultando el alumno {StudentId} en students-service", studentId);
            return null;
        }
    }

    // DTO interno para deserializar la respuesta del students-service
    private record StudentApiResponse(
        Guid    Id,
        string  FirstName,
        string  LastName,
        string  Email,
        string  EnrollmentNumber,
        bool    IsActive
    );
}
