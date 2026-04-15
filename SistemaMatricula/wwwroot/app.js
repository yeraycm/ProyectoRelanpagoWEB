// --- VARIABLES GLOBALES ---
let carrerasDisponibles = [];

// Iniciar al cargar la página
window.onload = async () => {
    await cargarCarreras(); // Primero cargamos carreras para que estén listas al llenar la tabla
    await cargarAspirantes();
};

/**
 * 1. Cargar carreras desde la BD para los selectores dinámicos
 */
async function cargarCarreras() {
    try {
        const response = await fetch('/api/Aspirantes/carreras');
        if (!response.ok) throw new Error();
        const data = await response.json();
        // Mapeamos para obtener solo los nombres si vienen como objetos
        carrerasDisponibles = data.map(c => c.nombre || c.Nombre || c);
    } catch (error) {
        console.error("Error al cargar carreras, usando respaldo local.");
        carrerasDisponibles = ["TI", "Ciberseguridad", "Redes"];
    }
}

/**
 * 2. Cargar la lista de aspirantes, actualizar tarjetas y renderizar tabla
 */
async function cargarAspirantes() {
    const email = document.getElementById('busquedaEmail').value;
    const estado = document.getElementById('filtroEstado').value;

    try {
        const res = await fetch(`/api/Aspirantes?email=${email}&estado=${estado}`);
        if (!res.ok) throw new Error("Error en la respuesta del servidor");

        const datos = await res.json();
        const tabla = document.getElementById('listaAspirantes');
        tabla.innerHTML = '';

        // --- ACTUALIZAR ESTADÍSTICAS ---
        document.getElementById('totalAspirantes').innerText = datos.length;
        document.getElementById('totalMatriculados').innerText = datos.filter(a => a.estaMatriculado).length;
        document.getElementById('totalNoMatriculados').innerText = datos.filter(a => !a.estaMatriculado).length;
        document.getElementById('contadorLista').innerText = `${datos.length} registros`;

        // --- RENDERIZAR FILAS ---
        datos.forEach(asp => {
            const tr = document.createElement('tr');

            // Generar opciones de carrera dinámicas basándose en la lista cargada
            let opcionesCarrera = "";
            carrerasDisponibles.forEach(c => {
                const seleccionado = (asp.carreraInteres === c) ? 'selected' : '';
                opcionesCarrera += `<option value="${c}" ${seleccionado}>${c}</option>`;
            });

            tr.innerHTML = `
                <td>${asp.nombreCompleto}</td>
                <td>${asp.email}</td>
                <td>
                    <select class="form-select form-select-sm border-0 bg-transparent" 
                            style="font-weight: 600; cursor: pointer;"
                            onchange="cambiarCarrera(${asp.idAspirante}, this.value)">
                        ${opcionesCarrera}
                    </select>
                </td>
                <td>
                    <span class="estado ${asp.estaMatriculado ? 'matriculado' : 'no-matriculado'}">
                        ${asp.estaMatriculado ? 'Matriculado' : 'No Matriculado'}
                    </span>
                </td>
                <td class="actions">
                    <button class="ver" onclick="verDetalleCita(${asp.idAspirante})" title="Ver Cita">
                        <i class="bi bi-calendar3"></i>
                    </button>
                    <button class="${asp.estaMatriculado ? 'desactivar' : 'activar'}" 
                            onclick="actualizarEstado(${asp.idAspirante}, ${!asp.estaMatriculado})" 
                            title="${asp.estaMatriculado ? 'Quitar Matrícula' : 'Marcar Matriculado'}">
                        <i class="bi ${asp.estaMatriculado ? 'bi-person-x' : 'bi-person-check'}"></i>
                    </button>
                </td>
            `;
            tabla.appendChild(tr);
        });
    } catch (err) {
        console.error("Error al cargar aspirantes:", err);
        mostrarNotificacion("Error al conectar con el servidor", "danger");
    }
}

/**
 * 3. Cambiar la carrera de un aspirante (PUT)
 */
async function cambiarCarrera(id, nuevaCarrera) {
    try {
        const response = await fetch(`/api/Aspirantes/${id}/carrera`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ NuevaCarrera: nuevaCarrera })
        });

        if (response.ok) {
            mostrarNotificacion("Carrera actualizada exitosamente", "success");
            // No recargamos toda la tabla para no perder el foco, a menos que sea necesario
        } else {
            throw new Error();
        }
    } catch {
        mostrarNotificacion("Error al actualizar carrera", "danger");
    }
}

/**
 * 4. Actualizar estado de matrícula (Toggle)
 */
async function actualizarEstado(id, nuevoEstado) {
    try {
        const res = await fetch(`/api/Aspirantes/${id}/matricula`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(nuevoEstado)
        });

        if (res.ok) {
            await cargarAspirantes(); // Refrescar tabla y contadores
            mostrarNotificacion("Estado de matrícula actualizado", "success");
        }
    } catch (err) {
        mostrarNotificacion("Error al actualizar el estado", "danger");
    }
}

/**
 * 5. Ver detalles de la cita en Modal
 */
async function verDetalleCita(id) {
    try {
        const respuesta = await fetch(`/api/Aspirantes/${id}/cita-detalle`);

        if (!respuesta.ok) {
            alert("Este aspirante aún no tiene una cita programada.");
            return;
        }

        const cita = await respuesta.json();

        // Llenar los datos en el modal
        document.getElementById('detNombre').innerText = cita.nombreCompleto;
        document.getElementById('detCarrera').innerText = cita.carrera;
        document.getElementById('detInicio').innerText = cita.horaInicio;
        document.getElementById('detFin').innerText = cita.horaFin;

        // Mostrar el modal de Bootstrap
        const modalElement = document.getElementById('modalDetalleCita');
        const myModal = new bootstrap.Modal(modalElement);
        myModal.show();

    } catch (error) {
        console.error("Error:", error);
        mostrarNotificacion("Error al obtener detalles de la cita", "danger");
    }
}

/**
 * Helper: Mostrar alertas visuales en el panel
 */
function mostrarNotificacion(msj, tipo) {
    const p = document.getElementById("panelMensajes");
    if (!p) return;

    p.innerHTML = `
        <div class="alert alert-${tipo} alert-dismissible fade show shadow-sm" style="border-radius: 15px;">
            <i class="bi ${tipo === 'success' ? 'bi-check-circle' : 'bi-exclamation-triangle'} me-2"></i>
            ${msj} 
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`;

    // Auto-eliminar después de 4 segundos
    setTimeout(() => { p.innerHTML = ""; }, 4000);
}