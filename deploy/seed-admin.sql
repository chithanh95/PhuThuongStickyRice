/* ============================================================================
   Tao tai khoan admin mac dinh cho Xoi Phu Thuong.
   Chay script nay trong SSMS (database PhuThuongStickyRice) neu trang /login
   bao "Ten dang nhap hoac mat khau khong dung" du da nhap dung.

   Tai khoan: admin@phuthuong.vn
   Mat khau:  Admin@123   <-- DOI NGAY sau khi dang nhap lan dau!

   PasswordHash ben duoi la hash chuan ASP.NET Identity cua "Admin@123",
   da kiem chung verify OK bang PasswordHasher that cua app.
   Script nay idempotent: chay lai nhieu lan van an toan.
   ============================================================================ */
USE PhuThuongStickyRice;
GO

-- 1) Role Admin
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = 'ADMIN')
BEGIN
    INSERT INTO dbo.Roles (Name, NormalizedName, ConcurrencyStamp, CreatedDateTime)
    VALUES ('Admin', 'ADMIN', CONVERT(nvarchar(64), NEWID()), SYSDATETIMEOFFSET());
    PRINT 'Role Admin created.';
END
ELSE
    PRINT 'Role Admin already exists.';
GO

-- 2) User admin@phuthuong.vn (upsert: dam bao hash luon dung)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE NormalizedEmail = 'ADMIN@PHUTHUONG.VN')
BEGIN
    INSERT INTO dbo.Users (
        UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
        CreatedDateTime
    )
    VALUES (
        'admin@phuthuong.vn', 'ADMIN@PHUTHUONG.VN', 'admin@phuthuong.vn', 'ADMIN@PHUTHUONG.VN', 1,
        'AQAAAAIAAYagAAAAEPmBkMo7WPVc+OMq81HKCP/xlSHRORCSeqZTMRG45UCZeBJTg9xhwa34a+9hgHR/hg==',
        CONVERT(nvarchar(64), NEWID()), CONVERT(nvarchar(64), NEWID()),
        0, 0, 1, 0,
        SYSDATETIMEOFFSET()
    );
    PRINT 'User admin@phuthuong.vn created.';
END
ELSE
BEGIN
    UPDATE dbo.Users
    SET PasswordHash = 'AQAAAAIAAYagAAAAEPmBkMo7WPVc+OMq81HKCP/xlSHRORCSeqZTMRG45UCZeBJTg9xhwa34a+9hgHR/hg==',
        SecurityStamp = CONVERT(nvarchar(64), NEWID()),
        LockoutEnabled = 1,
        LockoutEnd = NULL,
        AccessFailedCount = 0
    WHERE NormalizedEmail = 'ADMIN@PHUTHUONG.VN';
    PRINT 'User admin@phuthuong.vn already existed - password reset to Admin@123.';
END
GO

-- 3) Gan role Admin cho user
DECLARE @userId UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Users WHERE NormalizedEmail = 'ADMIN@PHUTHUONG.VN');
DECLARE @roleId UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Roles WHERE NormalizedName = 'ADMIN');
IF @userId IS NOT NULL AND @roleId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @userId AND RoleId = @roleId)
BEGIN
    INSERT INTO dbo.UserRoles (UserId, RoleId, CreatedDateTime)
    VALUES (@userId, @roleId, SYSDATETIMEOFFSET());
    PRINT 'Admin role assigned.';
END
ELSE
    PRINT 'Admin role already assigned.';
GO

SELECT Email AS AdminAccount FROM dbo.Users WHERE NormalizedEmail = 'ADMIN@PHUTHUONG.VN';
GO
