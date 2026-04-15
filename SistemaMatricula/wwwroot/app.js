// --- FUNCIONES DE INTERFAZ VISUAL ---

/**
 * Muestra un mensaje en un panel de notificaciones en lugar de usar alert().
 */
function mostrarMensaje(texto, tipo = 'danger') {
    const panel = document.getElementById("panelMensajes");
    if (!panel) return;

    panel.innerHTML = `
        <div class="alert alert-${tipo} alert-dismissible fade show shadow-sm" role="alert">
            <strong>${tipo === 'danger' ? '⚠️ Error:' : '✅ Éxito:'}</strong> ${texto}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>`;

    setTimeout(() => { panel.innerHTML = ""; }, 5000);
}

// --- FUNCIONES DE DATOS Y API ---

/**
 * Carga la lista de aspirantes y actualiza los contadores de las tarjetas.
 */
async function cargarAspirantes() {
    try {
        const email = document.getElementById("busquedaEmail").value;
        const estado = document.getElementById("filtroEstado").value;

        const response = await fetch(`/api/Aspirantes?email=${email}&estado=${estado}`);

        if (!response.ok) throw new Error("No se pudo cargar la lista.");

        const data = await response.json();
        const tabla = document.getElementById("listaAspirantes");
        tabla.innerHTML = "";

        // --- ACTUALIZACIÓN DE TARJETAS (DATOS REALES) ---
        document.getElementById("totalAspirantes").innerText = data.length;

        const matriculados = data.filter(asp => asp.estaMatriculado).length;
        document.getElementById("totalMatriculados").innerText = matriculados;

        const noMatriculados = data.filter(asp => !asp.estaMatriculado).length;
        document.getElementById("totalNoMatriculados").innerText = noMatriculados;

        // Actualizar contador pequeño de la tabla
        if (document.getElementById("contadorLista")) {
            document.getElementById("contadorLista").innerText = data.length;
        }

        // --- RENDERIZADO DE FILAS ---
        data.forEach(asp => {
            const claseEstado = asp.estaMatriculado ? 'matriculado' : 'no-matriculado';
            const textoEstado = asp.estaMatriculado ? 'Matriculado' : 'No Matriculado';

            tabla.innerHTML += `
                <tr>
                    <td>${asp.nombreCompleto}</td>
                    <td>${asp.email}</td>
                    <td>
                        <select class="form-select form-select-sm border-0 bg-transparent" onchange="cambiarCarrera(${asp.idAspirante}, this.value)">
                            <option value="TI" ${asp.carreraInteres === 'TI' ? 'selected' : ''}>TI</option>
                            <option value="Ciberseguridad" ${asp.carreraInteres === 'Ciberseguridad' ? 'selected' : ''}>Ciberseguridad</option>
                            <option value="Redes" ${asp.carreraInteres === 'Redes' ? 'selected' : ''}>Redes</option>
                        </select>
                    </td>
                    <td>
                        <span class="estado ${claseEstado}">${textoEstado}</span>
                    </td>
                    <td class="actions">
                        <button class="ver" onclick="verCita(${asp.idAspirante}, '${asp.nombreCompleto}')" title="Ver Cita">
                            <i class="bi bi-calendar-event"></i>
                        </button>
                        <button class="${asp.estaMatriculado ? 'desactivar' : 'activar'}" 
                                onclick="actualizarMatricula(${asp.idAspirante}, ${!asp.estaMatriculado})" 
                                title="${asp.estaMatriculado ? 'Quitar Matrícula' : 'Marcar Matriculado'}">
                            <i class="bi ${asp.estaMatriculado ? 'bi-person-x' : 'bi-person-check'}"></i>
                        </button>
                    </td>
                </tr>`;
        });
    } catch (error) {
        mostrarMensaje("Error al conectar con la base de datos de aspirantes.", 'danger');
    }
}

/**
 * Actualiza el estado de matrícula y refresca la lista/tarjetas.
 */
async function actualizarMatricula(id, estado) {
    try {
        const response = await fetch(`/api/Aspirantes/${id}/matricula`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(estado)
        });

        if (response.ok) {
            mostrarMensaje("Estado de matrícula actualizado.", 'success');
            cargarAspirantes(); // Refresca tarjetas y tabla
        } else {
            const result = await response.json();
            mostrarMensaje(result.error || "Error al actualizar matrícula.", 'danger');
        }
    } catch (error) {
        mostrarMensaje("No hay conexión con el servidor.", 'danger');
    }
}

/**
 * Cambia la carrera elegida por el aspirante.
 */
async function cambiarCarrera(id, nuevaCarrera) {
    try {
        const response = await fetch(`/api/Aspirantes/${id}/carrera`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ nuevaCarrera: nuevaCarrera })
        });

        const result = await response.json();

        if (response.ok) {
            mostrarMensaje(result.mensaje || "Carrera actualizada con éxito.", 'success');
            cargarAspirantes(); // Refresca por si hay filtros activos
        } else {
            mostrarMensaje(result.error || "No se pudo cambiar la carrera.", 'danger');
        }
    } catch (error) {
        mostrarMensaje("Error de red al cambiar carrera.", 'danger');
    }
}

/**
 * Obtiene y muestra la cita en el panel de mensajes.
 */
async function verCita(id, nombre) {
    try {
        const response = await fetch(`/api/Aspirantes/citas`);
        if (!response.ok) throw new Error();

        const citas = await response.json();
        const cita = citas.find(c => c.nombreCompleto === nombre);

        if (cita) {
            mostrarMensaje(`📅 Cita de ${nombre}: ${cita.horaInicio} hasta ${cita.horaFin}`, 'success');
        } else {
            mostrarMensaje(`El aspirante ${nombre} no tiene cita asignada aún.`, 'danger');
        }
    } catch (error) {
        mostrarMensaje("Error al obtener el listado de citas.", 'danger');
    }
}

// Carga inicial
window.onload = cargarAspirantes;