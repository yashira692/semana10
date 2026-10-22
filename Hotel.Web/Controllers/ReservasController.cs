using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel.Web.Controllers;

public class ReservasController : Controller
{
    private readonly ReservaRepositorio _repo;

    public ReservasController(ReservaRepositorio repo)
    {
        _repo = repo;
    }

    // GET: Reservas
    public async Task<IActionResult> Index()
    {
        var reservas = await _repo.ObtenerTodasAsync();
        return View(reservas);
    }

    // GET: Reservas/Disponibles?ingreso=2026-10-25&salida=2026-10-28
    public async Task<IActionResult> Disponibles(DateOnly? ingreso, DateOnly? salida)
    {
        ViewBag.Ingreso = ingreso;
        ViewBag.Salida = salida;
        ViewBag.Buscado = false;
        ViewBag.Noches = 0;

        var disponibles = new List<Habitacion>();

        if (ingreso != null && salida != null)
        {
            if (salida.Value <= ingreso.Value)
            {
                ViewBag.Error = "La fecha de salida debe ser posterior a la fecha de ingreso.";
            }
            else
            {
                disponibles = await _repo.ObtenerDisponiblesAsync(ingreso.Value, salida.Value);
                ViewBag.Noches = salida.Value.DayNumber - ingreso.Value.DayNumber;
                ViewBag.Buscado = true;
            }
        }

        return View(disponibles);
    }

    // GET: Reservas/Create
    public async Task<IActionResult> Create()
    {
        await CargarListasAsync();
        return View();
    }

    // POST: Reservas/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdHuesped,IdHabitacion,FechaIngreso,FechaSalida")] Reserva reserva)
    {
        // Estas propiedades no vienen del formulario
        ModelState.Remove(nameof(Reserva.IdHuespedNavigation));
        ModelState.Remove(nameof(Reserva.IdHabitacionNavigation));
        ModelState.Remove(nameof(Reserva.Estado));

        if (ModelState.IsValid)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            if (reserva.FechaSalida <= reserva.FechaIngreso)
            {
                ModelState.AddModelError(nameof(Reserva.FechaSalida), "La fecha de salida debe ser posterior a la fecha de ingreso.");
            }

            if (reserva.FechaIngreso < hoy)
            {
                ModelState.AddModelError(nameof(Reserva.FechaIngreso), "La fecha de ingreso no puede ser anterior a hoy.");
            }

            if (ModelState.IsValid &&
                await _repo.HabitacionOcupadaAsync(reserva.IdHabitacion, reserva.FechaIngreso, reserva.FechaSalida))
            {
                ModelState.AddModelError(nameof(Reserva.IdHabitacion), "La habitación ya está reservada en ese rango de fechas.");
            }
        }

        if (ModelState.IsValid)
        {
            await _repo.CrearAsync(reserva); // se guarda con Estado = "Pendiente"
            TempData["Mensaje"] = "La reserva se registró correctamente con estado Pendiente.";
            return RedirectToAction(nameof(Index)); // Post/Redirect/Get
        }

        await CargarListasAsync(reserva.IdHuesped, reserva.IdHabitacion);
        return View(reserva);
    }

    // POST: Reservas/Cancelar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var cancelada = await _repo.CancelarAsync(id);

        if (cancelada)
            TempData["Mensaje"] = "La reserva se canceló correctamente.";
        else
            TempData["Error"] = "No se pudo cancelar: la reserva no existe o no está Pendiente ni Confirmada.";

        return RedirectToAction(nameof(Index));
    }

    // ---------- Métodos privados ----------

    private async Task CargarListasAsync(int? idHuesped = null, int? idHabitacion = null)
    {
        var huespedes = await _repo.ObtenerHuespedesActivosAsync();
        var habitaciones = await _repo.ObtenerHabitacionesActivasAsync();

        ViewBag.Huespedes = new SelectList(
            huespedes.Select(h => new { h.IdHuesped, Texto = $"{h.Apellidos}, {h.Nombres} (DNI {h.Dni})" }),
            "IdHuesped", "Texto", idHuesped);

        ViewBag.Habitaciones = new SelectList(
            habitaciones.Select(h => new
            {
                h.IdHabitacion,
                Texto = $"{h.Numero} - {h.IdTipoHabitacionNavigation.Nombre} (S/ {h.IdTipoHabitacionNavigation.PrecioNoche:N2} por noche)"
            }),
            "IdHabitacion", "Texto", idHabitacion);
    }
}