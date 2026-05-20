using Microsoft.AspNetCore.Mvc;
using System.Runtime;

namespace AcademyManager.API.Controllers;

/// <summary>
/// Endpoint de diagnóstico interno del runtime .NET 8.
/// Expone información sobre el Garbage Collector, ThreadPool y memoria.
///
/// IMPORTANTE: proteger este endpoint en producción con autenticación
/// o restringirlo a la red interna del clúster (no exponer al exterior).
/// En Kubernetes, añadir una NetworkPolicy que solo permita acceso
/// desde el namespace de monitoring.
/// </summary>
[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController : ControllerBase
{
    // ── GC: modo y estado del Garbage Collector ───────────────────────────────
    [HttpGet("gc")]
    public IActionResult GetGcInfo()
    {
        return Ok(new
        {
            // ¿Está activo el modo servidor? Debe ser true en Kubernetes.
            // Si es false, ServerGarbageCollection no está funcionando.
            isServerGc = GCSettings.IsServerGC,

            // Modo de latencia: Batch (máximo throughput), Interactive, LowLatency...
            latencyMode = GCSettings.LatencyMode.ToString(),

            // Número de colecciones realizadas por generación desde el inicio
            collections = new
            {
                gen0 = GC.CollectionCount(0),
                gen1 = GC.CollectionCount(1),
                gen2 = GC.CollectionCount(2)
            },

            // Tamaño actual del heap en bytes (memoria gestionada por el GC)
            totalHeapBytes      = GC.GetTotalMemory(forceFullCollection: false),
            totalHeapMb         = Math.Round(GC.GetTotalMemory(forceFullCollection: false) / 1024.0 / 1024.0, 2),

            // Memoria total asignada acumulada (incluye objetos ya recolectados)
            totalAllocatedBytes = GC.GetTotalAllocatedBytes(),
            totalAllocatedMb    = Math.Round(GC.GetTotalAllocatedBytes() / 1024.0 / 1024.0, 2),

            // Información detallada por generación (tamaño del heap fragmentado)
            heapInfo = GC.GetGCMemoryInfo() is var info ? new
            {
                heapSizeBytes       = info.HeapSizeBytes,
                fragmentedBytes     = info.FragmentedBytes,
                memoryLoadBytes     = info.MemoryLoadBytes,
                highMemoryThreshold = info.HighMemoryLoadThresholdBytes,
                pauseTimePercent    = info.PauseTimePercentage
            } : null
        });
    }

    // ── ThreadPool: estado del pool de hilos ─────────────────────────────────
    [HttpGet("threadpool")]
    public IActionResult GetThreadPoolInfo()
    {
        ThreadPool.GetAvailableThreads(out var availableWorker, out var availableIo);
        ThreadPool.GetMaxThreads(out var maxWorker, out var maxIo);
        ThreadPool.GetMinThreads(out var minWorker, out var minIo);

        return Ok(new
        {
            workerThreads = new
            {
                available = availableWorker,
                max       = maxWorker,
                min       = minWorker,
                // En uso = max - available. Si se acerca a max, hay saturación.
                inUse     = maxWorker - availableWorker
            },
            ioCompletionThreads = new
            {
                available = availableIo,
                max       = maxIo,
                min       = minIo,
                inUse     = maxIo - availableIo
            },
            // Tareas pendientes en la cola del ThreadPool.
            // Un valor alto y creciente indica saturación o bloqueos en async/await.
            pendingWorkItems = ThreadPool.PendingWorkItemCount,
            completedWorkItems = ThreadPool.CompletedWorkItemCount
        });
    }

    // ── Memoria del proceso ───────────────────────────────────────────────────
    [HttpGet("memory")]
    public IActionResult GetMemoryInfo()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();

        return Ok(new
        {
            // Working Set: RAM física que el SO ha asignado al proceso
            workingSetMb     = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2),

            // Private Memory: memoria privada del proceso (no compartida con otros)
            privateMemoryMb  = Math.Round(process.PrivateMemorySize64 / 1024.0 / 1024.0, 2),

            // Virtual Memory: espacio de direcciones virtual total
            virtualMemoryMb  = Math.Round(process.VirtualMemorySize64 / 1024.0 / 1024.0, 2),

            // CPU acumulada desde el inicio del proceso
            totalCpuMs       = process.TotalProcessorTime.TotalMilliseconds,

            // Tiempo de vida del proceso
            uptimeSeconds    = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds
        });
    }

    // ── Resumen completo ──────────────────────────────────────────────────────
    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        ThreadPool.GetAvailableThreads(out var availableWorker, out _);
        ThreadPool.GetMaxThreads(out var maxWorker, out _);
        var process = System.Diagnostics.Process.GetCurrentProcess();

        return Ok(new
        {
            runtime = new
            {
                version        = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                isServerGc     = GCSettings.IsServerGC,
                environment    = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                processorCount = Environment.ProcessorCount
            },
            gc = new
            {
                isServerGc  = GCSettings.IsServerGC,
                gen0        = GC.CollectionCount(0),
                gen1        = GC.CollectionCount(1),
                gen2        = GC.CollectionCount(2),
                heapMb      = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2)
            },
            threadPool = new
            {
                inUse        = maxWorker - availableWorker,
                available    = availableWorker,
                pending      = ThreadPool.PendingWorkItemCount
            },
            memory = new
            {
                workingSetMb = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2),
                privateMemMb = Math.Round(process.PrivateMemorySize64 / 1024.0 / 1024.0, 2)
            }
        });
    }
}
