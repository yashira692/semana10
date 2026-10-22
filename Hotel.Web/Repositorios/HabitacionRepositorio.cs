using Hotel.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Web.Repositorios;

public class HabitacionRepositorio
{
    private readonly HotelContext _context;

    public HabitacionRepositorio(HotelContext context)
    {
        _context = context;
    }

    // Habitaciones activas con su tipo, filtradas por tipo (opcional) y ordenadas por número
    public async Task<List<Habitacion>> ObtenerActivasAsync(int? idTipo)
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo && (idTipo == null || h.IdTipoHabitacion == idTipo))
            .OrderBy(h => h.Numero)
            .ToListAsync();
    }

    // Una habitación activa con su tipo (para Details, Edit y Delete)
    public async Task<Habitacion?> ObtenerPorIdAsync(int id)
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .FirstOrDefaultAsync(h => h.IdHabitacion == id && h.Activo);
    }

    // Datos de la lista desplegable de tipos
    public async Task<List<TipoHabitacion>> ObtenerTiposAsync()
    {
        return await _context.TipoHabitacions
            .AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .ToListAsync();
    }

    // Revisa si el número ya existe (incluye habitaciones inactivas porque el índice único de la BD las cuenta)
    public async Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null)
    {
        return await _context.Habitacions
            .AnyAsync(h => h.Numero == numero && (excluirId == null || h.IdHabitacion != excluirId));
    }

    public async Task CrearAsync(Habitacion habitacion)
    {
        habitacion.Activo = true;
        _context.Habitacions.Add(habitacion);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ActualizarAsync(Habitacion datos)
    {
        var existente = await _context.Habitacions
            .FirstOrDefaultAsync(h => h.IdHabitacion == datos.IdHabitacion && h.Activo);

        if (existente == null)
        {
            return false;
        }

        existente.Numero = datos.Numero;
        existente.Piso = datos.Piso;
        existente.IdTipoHabitacion = datos.IdTipoHabitacion;

        await _context.SaveChangesAsync();
        return true;
    }

    // Eliminación lógica: Activo = 0, nunca un DELETE
    public async Task<bool> DesactivarAsync(int id)
    {
        var existente = await _context.Habitacions
            .FirstOrDefaultAsync(h => h.IdHabitacion == id && h.Activo);

        if (existente == null)
        {
            return false;
        }

        existente.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}