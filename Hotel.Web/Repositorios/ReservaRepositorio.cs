using Hotel.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Web.Repositorios;

public class ReservaRepositorio
{
    private readonly HotelContext _context;

    public ReservaRepositorio(HotelContext context)
    {
        _context = context;
    }

    // Todas las reservas con huésped, habitación y tipo (para calcular el total)
    public async Task<List<Reserva>> ObtenerTodasAsync()
    {
        return await _context.Reservas
            .AsNoTracking()
            .Include(r => r.IdHuespedNavigation)
            .Include(r => r.IdHabitacionNavigation)
                .ThenInclude(h => h.IdTipoHabitacionNavigation)
            .OrderByDescending(r => r.FechaIngreso)
            .ThenByDescending(r => r.IdReserva)
            .ToListAsync();
    }

    // Habitaciones activas libres en el rango, en UNA sola consulta (Any / !Any).
    // Una reserva se cruza si: Estado != Cancelada, FechaIngreso < salida pedida y FechaSalida > ingreso pedido.
    public async Task<List<Habitacion>> ObtenerDisponiblesAsync(DateOnly ingreso, DateOnly salida)
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo &&
                !h.Reservas.Any(r => r.Estado != "Cancelada" &&
                                     r.FechaIngreso < salida &&
                                     r.FechaSalida > ingreso))
            .OrderBy(h => h.Numero)
            .ToListAsync();
    }

    // ¿La habitación ya tiene una reserva que se cruza con el rango?
    public async Task<bool> HabitacionOcupadaAsync(int idHabitacion, DateOnly ingreso, DateOnly salida)
    {
        return await _context.Reservas
            .AnyAsync(r => r.IdHabitacion == idHabitacion &&
                           r.Estado != "Cancelada" &&
                           r.FechaIngreso < salida &&
                           r.FechaSalida > ingreso);
    }

    // Datos de las listas desplegables de Create
    public async Task<List<Huesped>> ObtenerHuespedesActivosAsync()
    {
        return await _context.Huespeds
            .AsNoTracking()
            .Where(h => h.Activo)
            .OrderBy(h => h.Apellidos)
            .ThenBy(h => h.Nombres)
            .ToListAsync();
    }

    public async Task<List<Habitacion>> ObtenerHabitacionesActivasAsync()
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo)
            .OrderBy(h => h.Numero)
            .ToListAsync();
    }

    public async Task CrearAsync(Reserva reserva)
    {
        reserva.Estado = "Pendiente";
        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();
    }

    // Solo se pueden cancelar las reservas Pendiente o Confirmada
    public async Task<bool> CancelarAsync(int id)
    {
        var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.IdReserva == id);

        if (reserva == null || (reserva.Estado != "Pendiente" && reserva.Estado != "Confirmada"))
        {
            return false;
        }

        reserva.Estado = "Cancelada";
        await _context.SaveChangesAsync();
        return true;
    }
}