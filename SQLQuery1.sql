-- 1️⃣ Create Database
CREATE DATABASE ClinicManagementDB;
GO
USE ClinicManagementDB;
GO

-- 2️⃣ Users Table (Admin & Regular Users)
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) UNIQUE NOT NULL,
    Password NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    Role NVARCHAR(50) CHECK (Role IN ('Admin', 'User')) NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    Address NVARCHAR(255) NULL
);
GO
INSERT INTO Users (Username, Password, Email, Role, FullName, PhoneNumber, Address)
VALUES 
('adm', 'admin123', 'adm@example.com', 'Admin', 'Admin', '03331234569', '789 Road, Islamabad');
('ali_khan', 'hashed_password1', 'ali@example.com', 'User', 'Ali Khan', '03123456789', '123 Street, Karachi'),
('sara_ahmed', 'hashed_password2', 'sara@example.com', 'User', 'Sara Ahmed', '03211223344', '456 Avenue, Lahore'),
('zain_malik', 'hashed_password3', 'zain@example.com', 'User', 'Zain Malik', '03331234567', '789 Road, Islamabad');

-- 3️⃣ Categories Table
CREATE TABLE Categories (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) UNIQUE NOT NULL
);
GO

-- 4️⃣ Products Table (Medicines & Machinery Combined)
CREATE TABLE Products (
    ProductID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    CategoryID INT FOREIGN KEY REFERENCES Categories(CategoryID) ON DELETE SET NULL,
    Price DECIMAL(10,2) NOT NULL CHECK (Price > 0),
    StockQuantity INT NOT NULL CHECK (StockQuantity >= 0),
    Manufacturer NVARCHAR(100) NULL,
    ImageURL NVARCHAR(255) NULL
);
GO

-- 5️⃣ Cart Table (User adds products before ordering)
CREATE TABLE Cart (
    CartID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID) ON DELETE CASCADE,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    AddedDate DATETIME DEFAULT GETDATE()
);
GO

-- 6️⃣ Orders Table (Stores user orders)
CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    OrderDate DATETIME DEFAULT GETDATE(),
    TotalAmount DECIMAL(10,2) NOT NULL CHECK (TotalAmount > 0),
    ShippingAddress NVARCHAR(255) NULL,
    OrderStatus NVARCHAR(50) CHECK (OrderStatus IN ('Pending', 'Processed', 'Shipped', 'Delivered')) NOT NULL DEFAULT 'Pending',
    PaymentMethod NVARCHAR(50) DEFAULT 'Cash on Delivery'
);
GO

-- 7️⃣ Order Details Table (Tracks ordered products)
CREATE TABLE OrderDetails (
    OrderDetailID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT FOREIGN KEY REFERENCES Orders(OrderID) ON DELETE CASCADE,
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID) ON DELETE CASCADE,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    Price DECIMAL(10,2) NOT NULL CHECK (Price > 0)
);
GO

-- 8️⃣ Insert Default Categories
INSERT INTO Categories (CategoryName) VALUES 
('Painkillers'), 
('Antibiotics'), 
('Medical Equipment'), 
('Diagnostics');
GO

-- 9️⃣ Insert Sample Users (Password should be hashed in real application)
INSERT INTO Users (Username, Password, Email, Role, FullName, PhoneNumber, Address)
VALUES 
('admin123', 'hashedpassword1', 'admin@example.com', 'Admin', 'Admin User', '03123456789', '123 Admin Street'),
('john_doe', 'hashedpassword2', 'john@example.com', 'User', 'John Doe', '03211234567', '456 User Lane');
GO

-- 🔟 Insert Sample Products (Medicines & Machinery with Categories)
INSERT INTO Products (Name, Description, CategoryID, Price, StockQuantity, Manufacturer, ImageURL)
VALUES 
('Paracetamol', 'Pain reliever', 1, 150.00, 50, 'XYZ Pharma', 'images/med1.jpg'),
('ECG Machine', 'Electrocardiography device', 3, 50000.00, 5, 'MediTech Ltd.', 'images/machine1.jpg');
GO

-- 1️⃣1️⃣ View Tables (To Check Data)
SELECT * FROM Users;
SELECT * FROM Categories;
SELECT * FROM Products;
SELECT * FROM Cart;
SELECT * FROM Orders;
SELECT * FROM OrderDetails;
SELECT * FROM Products WHERE ProductId = 2;

SELECT * FROM Orders;
INSERT INTO Orders (UserId, OrderDate, TotalAmount, ShippingAddress, OrderStatus, PaymentMethod) 
VALUES 
(3, GETDATE(), 1500.00, '123 Street, Karachi', 'Pending', 'Cash on Delivery'),
(4, GETDATE(), 3200.50, '456 Avenue, Lahore', 'Shipped', 'Credit Card'),
(5, GETDATE(), 500.75, '789 Road, Islamabad', 'Delivered', 'Cash on Delivery');

SELECT UserID, FullName FROM Users;
SELECT * FROM Orders WHERE UserID = 6;

INSERT INTO OrderDetails (OrderID, ProductID, Quantity, Price)
VALUES 
(4, 4, 3, 150.00),  -- Order 4, Product 2, 3 units, $150 each
(5, 5, 1, 250.00),  -- Order 5, Product 3, 1 unit, $250 each
(6, 6, 2, 100.00);  -- Order 6, Product 1, 2 units, $100 each

SELECT * FROM Products;
SELECT * FROM OrderDetails;

SELECT name, definition 
FROM sys.check_constraints 
WHERE parent_object_id = OBJECT_ID('Orders');
SELECT DISTINCT OrderStatus FROM Orders;

SELECT * FROM OrderDetails WHERE OrderID = 6;



-- 1️⃣2️⃣ Education Events Table (For Lectures, Seminars, etc.)
CREATE TABLE EducationEvents (
    EventID INT PRIMARY KEY IDENTITY(1,1),
    EventName NVARCHAR(255) NOT NULL,
    EventDate DATE NOT NULL,
    EventTime TIME NOT NULL,
    Speaker NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL
);
GO

-- 1️⃣3️⃣ Event Registrations Table (Users Register for Events)
CREATE TABLE EventRegistrations (
    RegistrationID INT PRIMARY KEY IDENTITY(1,1),
    EventID INT FOREIGN KEY REFERENCES EducationEvents(EventID) ON DELETE CASCADE,
    UserID INT FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    RegistrationDate DATETIME DEFAULT GETDATE()
);
GO

-- 1️⃣4️⃣ Insert Sample Education Events
INSERT INTO EducationEvents (EventName, EventDate, EventTime, Speaker, Description) 
VALUES 
('Basics of ECG', '2025-03-10', '14:00:00', 'Dr. Ali Raza', 'Understanding the basics of ECG readings and their interpretation.'),
('Antibiotics Awareness Seminar', '2025-03-15', '11:00:00', 'Dr. Ayesha Khan', 'Importance and correct use of antibiotics to prevent resistance.');
GO

-- 1️⃣5️⃣ Insert Sample Event Registrations
INSERT INTO EventRegistrations (EventID, UserID) 
VALUES 

(2, 3);  -- Zain Malik registered for Antibiotics Awareness Seminar
GO

-- 1️⃣6️⃣ View Data
SELECT * FROM EducationEvents;
SELECT * FROM EventRegistrations;


CREATE TABLE Feedback (
    FeedbackId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    UserId INT NOT NULL,
    Comment NVARCHAR(500) NOT NULL,
    Rating INT CHECK (Rating BETWEEN 1 AND 5) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
INSERT INTO Feedback (ProductId, UserId, Comment, Rating)
VALUES 
(5, 5, 'Decent quality, but could be improved.', 3)


SELECT f.FeedbackId, p.Name, u.FullName, f.Comment, f.Rating, f.CreatedAt
FROM Feedback f
JOIN Products p ON f.ProductId = p.ProductId
JOIN Users u ON f.UserId = u.UserId
ORDER BY f.CreatedAt DESC;


SELECT * FROM Feedback WHERE UserId = (SELECT UserId FROM Users WHERE Role = 'Admin');

_context.Feedbacks.Add(new Feedback
{
    ProductId = 5,  // Change as needed
    UserId = 5,     // Change as needed
    Comment = "Test feedback",
    Rating = 5,
    CreatedAt = DateTime.Now
});
_context.SaveChanges();


ALTER TABLE EducationEvents 
ADD EventImage NVARCHAR(255) NULL; -- Store image file path



UPDATE EducationEvents 
SET EventImage = 'images/ecg_basics.jpg' 
WHERE EventID = 1;

UPDATE EducationEvents 
SET EventImage = 'images/antibiotics_awareness.jpg' 
WHERE EventID = 2;

SELECT * FROM EducationEvents;

SELECT EventImage FROM EducationEvents;
SELECT * FROM Users WHERE Email IS NULL OR Password IS NULL;
