using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel.Web.Controllers;

public class HabitacionesController : Controller
{
    private readonly HabitacionRepositorio _repo;

    public HabitacionesController(HabitacionRepositorio repo)
    {
        _repo = repo;
    }

    // GET: Habitaciones?idTipo=1
    public async Task<IActionResult> Index(int? idTipo)
    {
        await CargarTiposAsync(idTipo);
        var habitaciones = await _repo.ObtenerActivasAsync(idTipo);
        return View(habitaciones);
    }

    // GET: Habitaciones/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var habitacion = await _repo.ObtenerPorIdAsync(id.Value);
        if (habitacion == null) return NotFound();

        return View(habitacion);
    }

    // GET: Habitaciones/Create
    public async Task<IActionResult> Create()
    {
        await CargarTiposAsync();
        return View();
    }

    // POST: Habitaciones/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Numero,Piso,IdTipoHabitacion")] Habitacion habitacion)
    {
        QuitarNavegacion();
        await ValidarNumeroAsync(habitacion);

        if (ModelState.IsValid)
        {
            await _repo.CrearAsync(habitacion);
            TempData["Mensaje"] = $"La habitación {habitacion.Numero} se creó correctamente.";
            return RedirectToAction(nameof(Index)); // Post/Redirect/Get
        }

        await CargarTiposAsync(habitacion.IdTipoHabitacion);
        return View(habitacion);
    }

    // GET: Habitaciones/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var habitacion = await _repo.ObtenerPorIdAsync(id.Value);
        if (habitacion == null) return NotFound();

        await CargarTiposAsync(habitacion.IdTipoHabitacion);
        return View(habitacion);
    }

    // POST: Habitaciones/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdHabitacion,Numero,Piso,IdTipoHabitacion")] Habitacion habitacion)
    {
        if (id != habitacion.IdHabitacion) return NotFound();

        QuitarNavegacion();
        await ValidarNumeroAsync(habitacion, habitacion.IdHabitacion);

        if (ModelState.IsValid)
        {
            var actualizada = await _repo.ActualizarAsync(habitacion);
            if (!actualizada) return NotFound();

            TempData["Mensaje"] = $"La habitación {habitacion.Numero} se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        await CargarTiposAsync(habitacion.IdTipoHabitacion);
        return View(habitacion);
    }

    // GET: Habitaciones/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var habitacion = await _repo.ObtenerPorIdAsync(id.Value);
        if (habitacion == null) return NotFound();

        return View(habitacion);
    }

    // POST: Habitaciones/Delete/5 (eliminación lógica: Activo = 0)
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var eliminada = await _repo.DesactivarAsync(id);

        if (eliminada)
            TempData["Mensaje"] = "La habitación se eliminó correctamente.";
        else
            TempData["Error"] = "No se pudo eliminar: la habitación no existe o ya fue eliminada.";

        return RedirectToAction(nameof(Index));
    }

    // ---------- Métodos privados ----------

    private async Task CargarTiposAsync(int? seleccionado = null)
    {
        var tipos = await _repo.ObtenerTiposAsync();
        ViewBag.Tipos = new SelectList(tipos, nameof(TipoHabitacion.IdTipoHabitacion), nameof(TipoHabitacion.Nombre), seleccionado);
    }

    // Las propiedades de navegación no vienen del formulario: se quitan del ModelState
    private void QuitarNavegacion()
    {
        ModelState.Remove(nameof(Habitacion.IdTipoHabitacionNavigation));
        ModelState.Remove(nameof(Habitacion.Reservas));
    }

    private async Task ValidarNumeroAsync(Habitacion habitacion, int? excluirId = null)
    {
        if (habitacion.Numero != null)
        {
            habitacion.Numero = habitacion.Numero.Trim();
        }

        if (string.IsNullOrWhiteSpace(habitacion.Numero))
        {
            return; // el [Required] implícito ya muestra el error
        }

        if (habitacion.Numero.Length > 10)
        {
            ModelState.AddModelError(nameof(Habitacion.Numero), "El número no puede tener más de 10 caracteres.");
        }
        else if (await _repo.ExisteNumeroAsync(habitacion.Numero, excluirId))
        {
            ModelState.AddModelError(nameof(Habitacion.Numero), "Ya existe una habitación con ese número.");
        }
    }
}