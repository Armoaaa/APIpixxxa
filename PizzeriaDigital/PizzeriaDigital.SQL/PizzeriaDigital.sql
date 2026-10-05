-- ============================================================
-- BASE DE DATOS - PizzeriaDigital
-- Mantiene los mismos nombres utilizados en C#
-- ============================================================

CREATE DATABASE IF NOT EXISTS PizzeriaDigital
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE PizzeriaDigital;


-- ============================================================
-- TABLA: Cliente
-- Corresponde a:
-- PizzeriaDigital.Shared.Models.Cliente
-- ============================================================

CREATE TABLE Cliente
(
    Id INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(150) NOT NULL,
    Telefono VARCHAR(50) NOT NULL,
    Direccion VARCHAR(250) NOT NULL,

    PRIMARY KEY (Id)
);


-- ============================================================
-- TABLA: Pizza
-- Corresponde a:
-- PizzeriaDigital.Shared.Models.Pizza
-- ============================================================

CREATE TABLE Pizza
(
    Id INT NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Tamano VARCHAR(50) NOT NULL,
    Precio DECIMAL(10,2) NOT NULL,

    PRIMARY KEY (Id),

    CONSTRAINT CK_Pizza_Precio
        CHECK (Precio >= 0)
);


-- ============================================================
-- TABLA: Ingredientes
--
-- En C# Pizza tiene:
--
-- List<string> Ingredientes
--
-- Como una lista no puede guardarse directamente como una
-- relación normal, se crea esta tabla.
-- ============================================================

CREATE TABLE Ingredientes
(
    Id INT NOT NULL AUTO_INCREMENT,
    PizzaId INT NOT NULL,
    Ingrediente VARCHAR(100) NOT NULL,

    PRIMARY KEY (Id),

    CONSTRAINT FK_Ingredientes_Pizza
        FOREIGN KEY (PizzaId)
        REFERENCES Pizza(Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);


-- ============================================================
-- TABLA: Pedido
-- Corresponde a:
-- PizzeriaDigital.Shared.Models.Pedido
-- ============================================================

CREATE TABLE Pedido
(
    Id INT NOT NULL AUTO_INCREMENT,
    ClienteId INT NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Corresponde a EstadoPedido:
    --
    -- 0 = EsperaConfirmacion
    -- 1 = EnPreparacion
    -- 2 = EnViaje
    -- 3 = Entregado

    Estado INT NOT NULL DEFAULT 0,

    ConError BOOLEAN NOT NULL DEFAULT FALSE,
    UltimoError TEXT NULL,

    PRIMARY KEY (Id),

    CONSTRAINT FK_Pedido_Cliente
        FOREIGN KEY (ClienteId)
        REFERENCES Cliente(Id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT CK_Pedido_Estado
        CHECK (Estado BETWEEN 0 AND 3)
);


-- ============================================================
-- TABLA: ItemPedido
-- Corresponde a:
-- PizzeriaDigital.Shared.Models.ItemPedido
-- ============================================================

CREATE TABLE ItemPedido
(
    PedidoId INT NOT NULL,
    PizzaId INT NOT NULL,

    NombrePizza VARCHAR(100) NOT NULL,
    Cantidad INT NOT NULL,
    Subtotal DECIMAL(10,2) NOT NULL,

    PRIMARY KEY (PedidoId, PizzaId),

    CONSTRAINT FK_ItemPedido_Pedido
        FOREIGN KEY (PedidoId)
        REFERENCES Pedido(Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT FK_ItemPedido_Pizza
        FOREIGN KEY (PizzaId)
        REFERENCES Pizza(Id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT CK_ItemPedido_Cantidad
        CHECK (Cantidad > 0),

    CONSTRAINT CK_ItemPedido_Subtotal
        CHECK (Subtotal >= 0)
);


-- ============================================================
-- ÍNDICES
-- ============================================================

CREATE INDEX IX_Pedido_ClienteId
ON Pedido(ClienteId);

CREATE INDEX IX_Pedido_Estado
ON Pedido(Estado);

CREATE INDEX IX_ItemPedido_PizzaId
ON ItemPedido(PizzaId);

CREATE INDEX IX_Ingredientes_PizzaId
ON Ingredientes(PizzaId);


-- ============================================================
-- PROCEDIMIENTO: Crear
-- ClienteRepository.Crear(...)
-- ============================================================

DELIMITER //

CREATE PROCEDURE CrearCliente
(
    IN p_Nombre VARCHAR(150),
    IN p_Telefono VARCHAR(50),
    IN p_Direccion VARCHAR(250)
)
BEGIN

    INSERT INTO Cliente
    (
        Nombre,
        Telefono,
        Direccion
    )
    VALUES
    (
        p_Nombre,
        COALESCE(p_Telefono, ''),
        p_Direccion
    );

    SELECT
        Id,
        Nombre,
        Telefono,
        Direccion
    FROM Cliente
    WHERE Id = LAST_INSERT_ID();

END //

DELIMITER ;


-- ============================================================
-- PROCEDIMIENTO: Obtener
-- ClienteRepository.Obtener(int id)
-- ============================================================

DELIMITER //

CREATE PROCEDURE ObtenerCliente
(
    IN p_Id INT
)
BEGIN

    SELECT
        Id,
        Nombre,
        Telefono,
        Direccion
    FROM Cliente
    WHERE Id = p_Id;

END //

DELIMITER ;


-- ============================================================
-- PROCEDIMIENTO: ObtenerTodas
-- PizzaRepository.ObtenerTodas()
--
-- Devuelve las pizzas y sus ingredientes.
-- ============================================================

DELIMITER //

CREATE PROCEDURE ObtenerTodas
()
BEGIN

    SELECT
        p.Id,
        p.Nombre,
        p.Tamano,
        p.Precio,
        COALESCE(
            GROUP_CONCAT(i.Ingrediente
                ORDER BY i.Id
                SEPARATOR ', '),
            ''
        ) AS Ingredientes

    FROM Pizza p

    LEFT JOIN Ingredientes i
        ON i.PizzaId = p.Id

    GROUP BY
        p.Id,
        p.Nombre,
        p.Tamano,
        p.Precio

    ORDER BY p.Id;

END //

DELIMITER ;


-- ============================================================
-- PROCEDIMIENTO: Obtener
-- PizzaRepository.Obtener(int id)
-- ============================================================

DELIMITER //

CREATE PROCEDURE ObtenerPizza
(
    IN p_Id INT
)
BEGIN

    SELECT
        p.Id,
        p.Nombre,
        p.Tamano,
        p.Precio,
        COALESCE(
            GROUP_CONCAT(
                i.Ingrediente
                ORDER BY i.Id
                SEPARATOR ', '
            ),
            ''
        ) AS Ingredientes

    FROM Pizza p

    LEFT JOIN Ingredientes i
        ON i.PizzaId = p.Id

    WHERE p.Id = p_Id

    GROUP BY
        p.Id,
        p.Nombre,
        p.Tamano,
        p.Precio;

END //

DELIMITER ;


-- ============================================================
-- PROCEDIMIENTO: CrearPedido
-- PedidoRepository.Crear(int clienteId, List<ItemPedido> items)
--
-- Recibe los Items como JSON.
--
-- Ejemplo:
--
-- [
--   {
--      "PizzaId": 1,
--      "Cantidad": 2
--   },
--   {
--      "PizzaId": 3,
--      "Cantidad": 1
--   }
-- ]
-- ============================================================

DELIMITER //

CREATE PROCEDURE CrearPedido
(
    IN p_ClienteId INT,
    IN p_Items JSON
)
BEGIN

    DECLARE v_PedidoId INT;

    START TRANSACTION;

    -- Verificar cliente
    IF NOT EXISTS
    (
        SELECT 1
        FROM Cliente
        WHERE Id = p_ClienteId
    )
    THEN

        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'No existe el cliente';

    END IF;


    -- Crear pedido
    INSERT INTO Pedido
    (
        ClienteId,
        FechaCreacion,
        Estado,
        ConError,
        UltimoError
    )
    VALUES
    (
        p_ClienteId,
        UTC_TIMESTAMP(),
        0,
        FALSE,
        NULL
    );

    SET v_PedidoId = LAST_INSERT_ID();


    -- Crear Items
    INSERT INTO ItemPedido
    (
        PedidoId,
        PizzaId,
        NombrePizza,
        Cantidad,
        Subtotal
    )
    SELECT
        v_PedidoId,
        jt.PizzaId,
        p.Nombre,
        jt.Cantidad,
        p.Precio * jt.Cantidad

    FROM JSON_TABLE
    (
        p_Items,
        '$[*]'
        COLUMNS
        (
            PizzaId INT PATH '$.PizzaId',
            Cantidad INT PATH '$.Cantidad'
        )
    ) AS jt

    INNER JOIN Pizza p
        ON p.Id = jt.PizzaId;


    COMMIT;


    -- Devolver el pedido creado
    CALL ObtenerPedido(v_PedidoId);

END //

DELIMITER ;


-- ============================================================
-- PROCEDIMIENTO: ObtenerPedido
-- PedidoRepository.Obtener(int id)
-- ============================================================

DELIMITER //

CREATE PROCEDURE ObtenerPedido
(
    IN p_Id INT
)
BEGIN

    -- Datos principales del Pedido
    SELECT
        p.Id,
        p.ClienteId,
        p.FechaCreacion,
        p.Estado,
        p.ConError,
        p.UltimoError,

        COALESCE(
            SUM(ip.Subtotal),
            0
        ) AS Total

    FROM Pedido p

    LEFT JOIN ItemPedido ip
        ON ip.PedidoId = p.Id

    WHERE p.Id = p_Id

    GROUP BY
        p.Id,
        p.ClienteId,
        p.FechaCreacion,
        p.Estado,
        p.ConError,
        p.UltimoError;


    -- Items del Pedido
    SELECT
        PedidoId,
        PizzaId,
        NombrePizza,
        Cantidad,
        Subtotal

    FROM ItemPedido

    WHERE PedidoId = p_Id;

END //

DELIMITER ;


-- ============================================================
-- PROCEDIMIENTO: ActualizarEstado
-- PedidoRepository.ActualizarEstado(...)
-- ============================================================

DELIMITER //

CREATE PROCEDURE ActualizarEstado
(
    IN p_Id INT,
    IN p_NuevoEstado INT
)
BEGIN

    IF p_NuevoEstado NOT BETWEEN 0 AND 3 THEN

        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'EstadoPedido invalido';

    END IF;


    UPDATE Pedido

    SET
        Estado = p_NuevoEstado,
        ConError = FALSE,
        UltimoError = NULL

    WHERE Id = p_Id;

END //

DELIMITER ;


-- ============================================================
-- PROCEDIMIENTO: MarcarError
-- PedidoRepository.MarcarError(...)
-- ============================================================

DELIMITER //

CREATE PROCEDURE MarcarError
(
    IN p_Id INT,
    IN p_Mensaje TEXT
)
BEGIN

    UPDATE Pedido

    SET
        ConError = TRUE,
        UltimoError = p_Mensaje

    WHERE Id = p_Id;

END //

DELIMITER ;


-- ============================================================
-- DATOS INICIALES
-- Son exactamente las pizzas del PizzaRepository
-- ============================================================

INSERT INTO Pizza
(
    Id,
    Nombre,
    Tamano,
    Precio
)
VALUES
(
    1,
    'Muzzarella',
    'Grande',
    8500.00
),
(
    2,
    'Napolitana',
    'Grande',
    9800.00
),
(
    3,
    'Fugazzeta',
    'Grande',
    9500.00
),
(
    4,
    'Especial',
    'Grande',
    11200.00
),
(
    5,
    'Cuatro Quesos',
    'Mediana',
    9900.00
);


-- ============================================================
-- INGREDIENTES
-- Son exactamente los del PizzaRepository
-- ============================================================

INSERT INTO Ingredientes
(
    PizzaId,
    Ingrediente
)
VALUES

-- Muzzarella
(1, 'Muzzarella'),
(1, 'Salsa de tomate'),
(1, 'Orégano'),

-- Napolitana
(2, 'Muzzarella'),
(2, 'Tomate'),
(2, 'Ajo'),
(2, 'Orégano'),

-- Fugazzeta
(3, 'Muzzarella'),
(3, 'Cebolla'),

-- Especial
(4, 'Muzzarella'),
(4, 'Jamón'),
(4, 'Morrones'),
(4, 'Aceitunas'),

-- Cuatro Quesos
(5, 'Muzzarella'),
(5, 'Provolone'),
(5, 'Roquefort'),
(5, 'Parmesano');


-- ============================================================
-- VERIFICACIÓN
-- ============================================================

SELECT * FROM Cliente;

SELECT * FROM Pizza;

SELECT * FROM Ingredientes;

SELECT * FROM Pedido;

SELECT * FROM ItemPedido;