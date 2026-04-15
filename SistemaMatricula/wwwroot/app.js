// --- VARIABLES GLOBALES ---
let carrerasDisponibles = [];

// --- FUNCIONES DE DATOS Y API ---

/**
 * Obtiene las carreras desde el endpoint correcto: /api/Aspirantes/carreras
 */
async function obtenerCarreras() {
    try {
        // CORRECCIÓN: La ruta según tu controlador es /api/Aspirantes/carreras
        const response = await fetch('/api/Aspirantes/carreras');

        if (!response.ok) throw new Error("Error en la respuesta del servidor");

        carrerasDisponibles = await response.json();
        console.log("Carreras cargadas desde la BD:", carrerasDisponibles);
    } catch (error) {
        console.error("No se pudieron cargar las carreras de la BD. Usando respaldo.");
        carrerasDisponibles = ["TI", "Ciberseguridad", "Redes"];
    }
}

/**
 * Carga la lista de aspirantes y genera los selectores dinámicamente.
 */
async function cargarAspirantes() {
    try {
        if (carrerasDisponibles.length === 0) {
            await obtenerCarreras();
        }

        const email = document.getElementById("busquedaEmail").value;
        const estado = document.getElementById("filtroEstado").value;

        // Endpoint para obtener aspirantes
        const response = await fetch(`/api/Aspirantes?email=${email}&estado=${estado}`);
        if (!response.ok) throw new Error("No se pudo cargar la lista.");

        const data = await response.json();
        const tabla = document.getElementById("listaAspirantes");
        tabla.innerHTML = "";

        // Actualizar Tarjetas
        document.getElementById("totalAspirantes").innerText = data.length;
        document.getElementById("totalMatriculados").innerText = data.filter(asp => asp.estaMatriculado).length;
        document.getElementById("totalNoMatriculados").innerText = data.filter(asp => !asp.estaMatriculado).length;

        data.forEach(asp => {
            const claseEstado = asp.estaMatriculado ? 'matriculado' : 'no-matriculado';
            const textoEstado = asp.estaMatriculado ? 'Matriculado' : 'No Matriculado';

            // GENERACIÓN DINÁMICA DE OPCIONES
            let opcionesCarrera = "";
            carrerasDisponibles.forEach(carrera => {
                // Ajustamos por si el objeto viene con 'nombre' o 'nombreCarrera'
                const nombreC = carrera.nombre || carrera.nombreCarrera || carrera;
                const seleccionado = asp.carreraInteres === nombreC ? 'selected' : '';
                opcionesCarrera += `<option value="${nombreC}" ${seleccionado}>${nombreC}</option>`;
            });

            tabla.innerHTML += `
                <tr>
                    <td>${asp.nombreCompleto}</td>
                    <td>${asp.email}</td>
                    <td>
                        <select class="form-select form-select-sm border-0 bg-transparent" onchange="cambiarCarrera(${asp.idAspirante}, this.value)">
                            ${opcionesCarrera}
                        </select>
                    </td>
                    <td><span class="estado ${claseEstado}">${textoEstado}</span></td>
                    <td class="actions">
                        <button class="ver" onclick="verCita(${asp.idAspirante}, '${asp.nombreCompleto}')"><i class="bi bi-calendar-event"></i></button>
                        <button class="${asp.estaMatriculado ? 'desactivar' : 'activar'}" 
                                onclick="actualizarMatricula(${asp.idAspirante}, ${!asp.estaMatriculado})">
                            <i class="bi ${asp.estaMatriculado ? 'bi-person-x' : 'bi-person-check'}"></i>
                        </button>
                    </td>
                </tr>`;
        });
    } catch (error) {
        console.error("Error al cargar aspirantes:", error);
    }
}

// ... (las demás funciones de actualizarMatricula y cambiarCarrera quedan igual)

window.onload = async () => {
    await obtenerCarreras();
    await cargarAspirantes();
};