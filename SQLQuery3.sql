-- 2. Récupération des IDs
DECLARE @AdminRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin');
DECLARE @UserId NVARCHAR(450) = (SELECT Id FROM AspNetUsers WHERE Email = 'admin@nexascore.com'); -- <--- TON EMAIL ICI

-- 3. Assignation du rôle (Liaison)
IF @AdminRoleId IS NOT NULL AND @UserId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @AdminRoleId)
    BEGIN
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        VALUES (@UserId, @AdminRoleId);
        PRINT 'Succès : L''utilisateur est maintenant Admin !';
    END
    ELSE
    BEGIN
        PRINT 'Info : Cet utilisateur est déjà Admin.';
    END
END
ELSE
BEGIN
    PRINT 'Erreur : Utilisateur ou Rôle introuvable.';
END