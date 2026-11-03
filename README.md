# Qute Aguilares – Login y Pantalla Principal (Windows Forms, .NET 8)

## Cómo abrirlo
1. Visual Studio 2022 con la carga de trabajo "Desarrollo de escritorio .NET".
2. Archivo > Abrir > Proyecto/Solución > `QuteApp.csproj`.
3. F5.

## Usuarios de demostración
| Rol | Teléfono | Contraseña |
|---|---|---|
| Administrador | 70000000 | admin123 |
| Pasajero | 71111111 | 123456 |
| Conductor (aprobado) | 72222222 | 123456 |
| Conductor (pendiente) | 73333333 | 123456 |

## Estructura
- `Forms/` FrmLogin, FrmRegistroPasajero, FrmRegistroConductor, FrmPrincipal
- `Vistas/` secciones del área de contenido (Pasajero, Conductor, Admin, comunes)
- `Servicios/` AuthService (datos en memoria) y datos demo
- `UI/` tema, tarjetas, mapa de demostración, gráfico de barras

## Conectar con EF Core / SQL Server
Reemplaza el interior de `AuthService` (y los datos de `Demo.cs`) por consultas a tu `DbContext`.
Las pantallas no necesitan cambios.
