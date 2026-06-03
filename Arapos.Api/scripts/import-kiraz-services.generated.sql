SET NOCOUNT ON;
DECLARE @TenantId uniqueidentifier = '2cc3c972-8e4b-424e-b3b8-33ac7b6acdff';
DECLARE @BranchId uniqueidentifier = '852e29aa-3ae9-43bd-9c04-13be9506fbb0';

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'إزالة لون جلش'
      AND ISNULL(NameEn, '') = 'Gelish color removing'
)
BEGIN
    DECLARE @ServiceId1 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId1 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId1, @TenantId, @BranchId, N'إزالة لون جلش', 'Gelish color removing', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId1, @TenantId, @BranchId, @ServiceId1, 3000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'أكريليك'
      AND ISNULL(NameEn, '') = 'Acryle'
)
BEGIN
    DECLARE @ServiceId2 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId2 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId2, @TenantId, @BranchId, N'أكريليك', 'Acryle', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId2, @TenantId, @BranchId, @ServiceId2, 25000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'ألوان فاشن مجنونة'
      AND ISNULL(NameEn, '') = 'Fashion crazy color'
)
BEGIN
    DECLARE @ServiceId3 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId3 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId3, @TenantId, @BranchId, N'ألوان فاشن مجنونة', 'Fashion crazy color', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId3, @TenantId, @BranchId, @ServiceId3, 25000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'أولابلكس'
      AND ISNULL(NameEn, '') = 'Olaplix'
)
BEGIN
    DECLARE @ServiceId4 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId4 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId4, @TenantId, @BranchId, N'أولابلكس', 'Olaplix', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId4, @TenantId, @BranchId, @ServiceId4, 15000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'بديكير'
      AND ISNULL(NameEn, '') = 'Pedicure'
)
BEGIN
    DECLARE @ServiceId5 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId5 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId5, @TenantId, @BranchId, N'بديكير', 'Pedicure', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId5, @TenantId, @BranchId, @ServiceId5, 6000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'بديكير + بارافين'
      AND ISNULL(NameEn, '') = 'Pedicure + Parafin'
)
BEGIN
    DECLARE @ServiceId6 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId6 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId6, @TenantId, @BranchId, N'بديكير + بارافين', 'Pedicure + Parafin', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId6, @TenantId, @BranchId, @ServiceId6, 8000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'بروتين جذور'
      AND ISNULL(NameEn, '') = 'Root protein'
)
BEGIN
    DECLARE @ServiceId7 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId7 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId7, @TenantId, @BranchId, N'بروتين جذور', 'Root protein', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId7, @TenantId, @BranchId, @ServiceId7, 25000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'بروتين شعر'
      AND ISNULL(NameEn, '') = 'Hair protein'
)
BEGIN
    DECLARE @ServiceId8 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId8 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId8, @TenantId, @BranchId, N'بروتين شعر', 'Hair protein', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId8, @TenantId, @BranchId, @ServiceId8, 50000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'بولي جل'
      AND ISNULL(NameEn, '') = 'Polygel'
)
BEGIN
    DECLARE @ServiceId9 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId9 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId9, @TenantId, @BranchId, N'بولي جل', 'Polygel', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId9, @TenantId, @BranchId, @ServiceId9, 25000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'تركيب أظافر عادي'
      AND ISNULL(NameEn, '') = 'Nail extension normal'
)
BEGIN
    DECLARE @ServiceId10 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId10 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId10, @TenantId, @BranchId, N'تركيب أظافر عادي', 'Nail extension normal', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId10, @TenantId, @BranchId, @ServiceId10, 10000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'تركيب جل'
      AND ISNULL(NameEn, '') = 'Gel extension'
)
BEGIN
    DECLARE @ServiceId11 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId11 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId11, @TenantId, @BranchId, N'تركيب جل', 'Gel extension', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId11, @TenantId, @BranchId, @ServiceId11, 25000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'تركيب جل مع تصميم تيب'
      AND ISNULL(NameEn, '') = 'Gel extension Tip Design'
)
BEGIN
    DECLARE @ServiceId12 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId12 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId12, @TenantId, @BranchId, N'تركيب جل مع تصميم تيب', 'Gel extension Tip Design', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId12, @TenantId, @BranchId, @ServiceId12, 30000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'تركيب شعر بالخياطة (خصلة)'
      AND ISNULL(NameEn, '') = 'Sewing hair extinction (the tuf)'
)
BEGIN
    DECLARE @ServiceId13 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId13 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId13, @TenantId, @BranchId, N'تركيب شعر بالخياطة (خصلة)', 'Sewing hair extinction (the tuf)', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId13, @TenantId, @BranchId, @ServiceId13, 7500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'تركيب شعر ستيكر (خصلة)'
      AND ISNULL(NameEn, '') = 'Stokens hair extinction (the tuf)'
)
BEGIN
    DECLARE @ServiceId14 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId14 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId14, @TenantId, @BranchId, N'تركيب شعر ستيكر (خصلة)', 'Stokens hair extinction (the tuf)', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId14, @TenantId, @BranchId, @ServiceId14, 6000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'تسريحة شعر طويل'
      AND ISNULL(NameEn, '') = 'Long hair styling'
)
BEGIN
    DECLARE @ServiceId15 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId15 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId15, @TenantId, @BranchId, N'تسريحة شعر طويل', 'Long hair styling', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId15, @TenantId, @BranchId, @ServiceId15, 25000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'تسريحة شعر قصير'
      AND ISNULL(NameEn, '') = 'Short hair styling'
)
BEGIN
    DECLARE @ServiceId16 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId16 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId16, @TenantId, @BranchId, N'تسريحة شعر قصير', 'Short hair styling', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId16, @TenantId, @BranchId, @ServiceId16, 15000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'تسريحة شعر متوسط'
      AND ISNULL(NameEn, '') = 'Medium hair styling'
)
BEGIN
    DECLARE @ServiceId17 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId17 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId17, @TenantId, @BranchId, N'تسريحة شعر متوسط', 'Medium hair styling', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId17, @TenantId, @BranchId, @ServiceId17, 20000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'تعبئة رموش'
      AND ISNULL(NameEn, '') = 'Refilling eyelashes'
)
BEGIN
    DECLARE @ServiceId18 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId18 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId18, @TenantId, @BranchId, N'تعبئة رموش', 'Refilling eyelashes', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId18, @TenantId, @BranchId, @ServiceId18, 14000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'تنظيف وتجهيز للتركيب'
      AND ISNULL(NameEn, '') = 'Cleansing and prepare the extinction'
)
BEGIN
    DECLARE @ServiceId19 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId19 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId19, @TenantId, @BranchId, N'تنظيف وتجهيز للتركيب', 'Cleansing and prepare the extinction', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId19, @TenantId, @BranchId, @ServiceId19, 15000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'جلش'
      AND ISNULL(NameEn, '') = 'Golish'
)
BEGIN
    DECLARE @ServiceId20 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId20 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId20, @TenantId, @BranchId, N'جلش', 'Golish', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId20, @TenantId, @BranchId, @ServiceId20, 16000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حلاوة البطن'
      AND ISNULL(NameEn, '') = 'Sugar stomach'
)
BEGIN
    DECLARE @ServiceId21 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId21 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId21, @TenantId, @BranchId, N'حلاوة البطن', 'Sugar stomach', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId21, @TenantId, @BranchId, @ServiceId21, 5000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حلاوة البكيني'
      AND ISNULL(NameEn, '') = 'Sugar bikini'
)
BEGIN
    DECLARE @ServiceId22 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId22 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId22, @TenantId, @BranchId, N'حلاوة البكيني', 'Sugar bikini', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId22, @TenantId, @BranchId, @ServiceId22, 9000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حلاوة الظهر'
      AND ISNULL(NameEn, '') = 'Sugar back'
)
BEGIN
    DECLARE @ServiceId23 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId23 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId23, @TenantId, @BranchId, N'حلاوة الظهر', 'Sugar back', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId23, @TenantId, @BranchId, @ServiceId23, 5000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حلاوة تحت الإبط'
      AND ISNULL(NameEn, '') = 'Sugar under arm'
)
BEGIN
    DECLARE @ServiceId24 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId24 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId24, @TenantId, @BranchId, N'حلاوة تحت الإبط', 'Sugar under arm', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId24, @TenantId, @BranchId, @ServiceId24, 3000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حلاوة جسم كامل بدون بكيني'
      AND ISNULL(NameEn, '') = 'Sugar full body without bikini'
)
BEGIN
    DECLARE @ServiceId25 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId25 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId25, @TenantId, @BranchId, N'حلاوة جسم كامل بدون بكيني', 'Sugar full body without bikini', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId25, @TenantId, @BranchId, @ServiceId25, 30000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حلاوة رجل كاملة'
      AND ISNULL(NameEn, '') = 'Sugar full leg'
)
BEGIN
    DECLARE @ServiceId26 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId26 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId26, @TenantId, @BranchId, N'حلاوة رجل كاملة', 'Sugar full leg', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId26, @TenantId, @BranchId, @ServiceId26, 9000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حلاوة نصف رجل'
      AND ISNULL(NameEn, '') = 'Sugar half leg'
)
BEGIN
    DECLARE @ServiceId27 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId27 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId27, @TenantId, @BranchId, N'حلاوة نصف رجل', 'Sugar half leg', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId27, @TenantId, @BranchId, @ServiceId27, 4500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حلاوة نصف يد'
      AND ISNULL(NameEn, '') = 'Sugar half hand'
)
BEGIN
    DECLARE @ServiceId28 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId28 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId28, @TenantId, @BranchId, N'حلاوة نصف يد', 'Sugar half hand', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId28, @TenantId, @BranchId, @ServiceId28, 4500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حلاوة يد كاملة'
      AND ISNULL(NameEn, '') = 'Sugar full hand'
)
BEGIN
    DECLARE @ServiceId29 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId29 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId29, @TenantId, @BranchId, N'حلاوة يد كاملة', 'Sugar full hand', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId29, @TenantId, @BranchId, @ServiceId29, 9000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حمام زيت عادي'
      AND ISNULL(NameEn, '') = 'Normal hot oil'
)
BEGIN
    DECLARE @ServiceId30 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId30 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId30, @TenantId, @BranchId, N'حمام زيت عادي', 'Normal hot oil', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId30, @TenantId, @BranchId, @ServiceId30, 8000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حمام زيت كيراز'
      AND ISNULL(NameEn, '') = 'Kiraz hot oil'
)
BEGIN
    DECLARE @ServiceId31 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId31 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId31, @TenantId, @BranchId, N'حمام زيت كيراز', 'Kiraz hot oil', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId31, @TenantId, @BranchId, @ServiceId31, 12000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حمام مغربي تبييض'
      AND ISNULL(NameEn, '') = 'Whitening Moroccan bath'
)
BEGIN
    DECLARE @ServiceId32 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId32 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId32, @TenantId, @BranchId, N'حمام مغربي تبييض', 'Whitening Moroccan bath', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId32, @TenantId, @BranchId, @ServiceId32, 22500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حمام مغربي خاص'
      AND ISNULL(NameEn, '') = 'Special Moroccan bath'
)
BEGIN
    DECLARE @ServiceId33 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId33 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId33, @TenantId, @BranchId, N'حمام مغربي خاص', 'Special Moroccan bath', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId33, @TenantId, @BranchId, @ServiceId33, 15000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حمام مغربي عادي'
      AND ISNULL(NameEn, '') = 'Moroccan bath normal'
)
BEGIN
    DECLARE @ServiceId34 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId34 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId34, @TenantId, @BranchId, N'حمام مغربي عادي', 'Moroccan bath normal', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId34, @TenantId, @BranchId, @ServiceId34, 7500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'حمام مغربي ملكي'
      AND ISNULL(NameEn, '') = 'Royal Moroccan bath'
)
BEGIN
    DECLARE @ServiceId35 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId35 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId35, @TenantId, @BranchId, N'حمام مغربي ملكي', 'Royal Moroccan bath', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId35, @TenantId, @BranchId, @ServiceId35, 30000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'دلكة سودانية 3 مناطق'
      AND ISNULL(NameEn, '') = 'Sudanese Delka 3 Parts'
)
BEGIN
    DECLARE @ServiceId36 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId36 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId36, @TenantId, @BranchId, N'دلكة سودانية 3 مناطق', 'Sudanese Delka 3 Parts', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId36, @TenantId, @BranchId, @ServiceId36, 15000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'دلكة سودانية جسم كامل'
      AND ISNULL(NameEn, '') = 'Full body Sudanese Dalka'
)
BEGIN
    DECLARE @ServiceId37 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId37 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId37, @TenantId, @BranchId, N'دلكة سودانية جسم كامل', 'Full body Sudanese Dalka', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId37, @TenantId, @BranchId, @ServiceId37, 30000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'رموش أسبوعية'
      AND ISNULL(NameEn, '') = 'Lashes live Pill weekly'
)
BEGIN
    DECLARE @ServiceId38 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId38 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId38, @TenantId, @BranchId, N'رموش أسبوعية', 'Lashes live Pill weekly', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId38, @TenantId, @BranchId, @ServiceId38, 12000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'رموش شهرية'
      AND ISNULL(NameEn, '') = 'Monthly eyelashes'
)
BEGIN
    DECLARE @ServiceId39 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId39 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId39, @TenantId, @BranchId, N'رموش شهرية', 'Monthly eyelashes', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId39, @TenantId, @BranchId, @ServiceId39, 35000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'رموش عادية'
      AND ISNULL(NameEn, '') = 'Normal eyelashes'
)
BEGIN
    DECLARE @ServiceId40 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId40 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId40, @TenantId, @BranchId, N'رموش عادية', 'Normal eyelashes', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId40, @TenantId, @BranchId, @ServiceId40, 5000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'رموش كيراز'
      AND ISNULL(NameEn, '') = 'Kiraz eyelashes'
)
BEGIN
    DECLARE @ServiceId41 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId41 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId41, @TenantId, @BranchId, N'رموش كيراز', 'Kiraz eyelashes', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId41, @TenantId, @BranchId, @ServiceId41, 8000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'رنساج'
      AND ISNULL(NameEn, '') = 'Rinse'
)
BEGIN
    DECLARE @ServiceId42 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId42 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId42, @TenantId, @BranchId, N'رنساج', 'Rinse', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId42, @TenantId, @BranchId, @ServiceId42, 20000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'سشوار شعر طويل'
      AND ISNULL(NameEn, '') = 'Blow dry long hair'
)
BEGIN
    DECLARE @ServiceId43 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId43 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId43, @TenantId, @BranchId, N'سشوار شعر طويل', 'Blow dry long hair', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId43, @TenantId, @BranchId, @ServiceId43, 12000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'سشوار شعر طويل جداً'
      AND ISNULL(NameEn, '') = 'Blow very long hair'
)
BEGIN
    DECLARE @ServiceId44 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId44 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId44, @TenantId, @BranchId, N'سشوار شعر طويل جداً', 'Blow very long hair', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId44, @TenantId, @BranchId, @ServiceId44, 20000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'سشوار شعر قصير'
      AND ISNULL(NameEn, '') = 'Blow dry short hair'
)
BEGIN
    DECLARE @ServiceId45 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId45 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId45, @TenantId, @BranchId, N'سشوار شعر قصير', 'Blow dry short hair', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId45, @TenantId, @BranchId, @ServiceId45, 7000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'سشوار شعر متوسط'
      AND ISNULL(NameEn, '') = 'Blow dry average hair'
)
BEGIN
    DECLARE @ServiceId46 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId46 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId46, @TenantId, @BranchId, N'سشوار شعر متوسط', 'Blow dry average hair', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId46, @TenantId, @BranchId, @ServiceId46, 8000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'سكراب جسم كامل بدون بكيني'
      AND ISNULL(NameEn, '') = 'Scrap full body without bikini'
)
BEGIN
    DECLARE @ServiceId47 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId47 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId47, @TenantId, @BranchId, N'سكراب جسم كامل بدون بكيني', 'Scrap full body without bikini', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId47, @TenantId, @BranchId, @ServiceId47, 25000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'سكراب رجل كاملة'
      AND ISNULL(NameEn, '') = 'Scrap full leg'
)
BEGIN
    DECLARE @ServiceId48 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId48 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId48, @TenantId, @BranchId, N'سكراب رجل كاملة', 'Scrap full leg', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId48, @TenantId, @BranchId, @ServiceId48, 8000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'سكراب نصف رجل'
      AND ISNULL(NameEn, '') = 'Scrap half leg'
)
BEGIN
    DECLARE @ServiceId49 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId49 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId49, @TenantId, @BranchId, N'سكراب نصف رجل', 'Scrap half leg', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId49, @TenantId, @BranchId, @ServiceId49, 4000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'سكراب نصف يد'
      AND ISNULL(NameEn, '') = 'Scrap half hand'
)
BEGIN
    DECLARE @ServiceId50 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId50 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId50, @TenantId, @BranchId, N'سكراب نصف يد', 'Scrap half hand', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId50, @TenantId, @BranchId, @ServiceId50, 4000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'سكراب يد كاملة'
      AND ISNULL(NameEn, '') = 'Scrap full hand'
)
BEGIN
    DECLARE @ServiceId51 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId51 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId51, @TenantId, @BranchId, N'سكراب يد كاملة', 'Scrap full hand', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId51, @TenantId, @BranchId, @ServiceId51, 8000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'صبغة جذور'
      AND ISNULL(NameEn, '') = 'Root hair color'
)
BEGIN
    DECLARE @ServiceId52 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId52 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId52, @TenantId, @BranchId, N'صبغة جذور', 'Root hair color', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId52, @TenantId, @BranchId, @ServiceId52, 10000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'صبغة شعر'
      AND ISNULL(NameEn, '') = 'Hair color'
)
BEGIN
    DECLARE @ServiceId53 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId53 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId53, @TenantId, @BranchId, N'صبغة شعر', 'Hair color', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId53, @TenantId, @BranchId, @ServiceId53, 20000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'علاج فيتامين "كنزارة"'
      AND ISNULL(NameEn, '') = 'Vitamin''s treatment "kanzara"'
)
BEGIN
    DECLARE @ServiceId54 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId54 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId54, @TenantId, @BranchId, N'علاج فيتامين "كنزارة"', 'Vitamin''s treatment "kanzara"', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId54, @TenantId, @BranchId, @ServiceId54, 25000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'علاج ماسك كيراز'
      AND ISNULL(NameEn, '') = 'Kiraz mask treatment'
)
BEGIN
    DECLARE @ServiceId55 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId55 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId55, @TenantId, @BranchId, N'علاج ماسك كيراز', 'Kiraz mask treatment', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId55, @TenantId, @BranchId, @ServiceId55, 18000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'فرنش قدم'
      AND ISNULL(NameEn, '') = 'Fronch foot'
)
BEGIN
    DECLARE @ServiceId56 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId56 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId56, @TenantId, @BranchId, N'فرنش قدم', 'Fronch foot', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId56, @TenantId, @BranchId, @ServiceId56, 3000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'فرنش يد'
      AND ISNULL(NameEn, '') = 'Fronch hand'
)
BEGIN
    DECLARE @ServiceId57 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId57 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId57, @TenantId, @BranchId, N'فرنش يد', 'Fronch hand', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId57, @TenantId, @BranchId, @ServiceId57, 3000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'فيشل عادي'
      AND ISNULL(NameEn, '') = 'Normal facial'
)
BEGIN
    DECLARE @ServiceId58 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId58 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId58, @TenantId, @BranchId, N'فيشل عادي', 'Normal facial', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId58, @TenantId, @BranchId, @ServiceId58, 20000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'قص أطراف الشعر'
      AND ISNULL(NameEn, '') = 'Hair trim'
)
BEGIN
    DECLARE @ServiceId59 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId59 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId59, @TenantId, @BranchId, N'قص أطراف الشعر', 'Hair trim', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId59, @TenantId, @BranchId, @ServiceId59, 3500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'قص شعر كامل'
      AND ISNULL(NameEn, '') = 'Hair cut'
)
BEGIN
    DECLARE @ServiceId60 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId60 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId60, @TenantId, @BranchId, N'قص شعر كامل', 'Hair cut', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId60, @TenantId, @BranchId, @ServiceId60, 8000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'قص غرة'
      AND ISNULL(NameEn, '') = 'Fringe cut'
)
BEGIN
    DECLARE @ServiceId61 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId61 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId61, @TenantId, @BranchId, N'قص غرة', 'Fringe cut', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId61, @TenantId, @BranchId, @ServiceId61, 3000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'لون أظافر قدم'
      AND ISNULL(NameEn, '') = 'Foot nail color'
)
BEGIN
    DECLARE @ServiceId62 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId62 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId62, @TenantId, @BranchId, N'لون أظافر قدم', 'Foot nail color', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId62, @TenantId, @BranchId, @ServiceId62, 2000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'لون أظافر يد'
      AND ISNULL(NameEn, '') = 'Hand nail color'
)
BEGIN
    DECLARE @ServiceId63 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId63 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId63, @TenantId, @BranchId, N'لون أظافر يد', 'Hand nail color', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId63, @TenantId, @BranchId, @ServiceId63, 2000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'ماسك شعر كيراز'
      AND ISNULL(NameEn, '') = 'Kiraz hair mask'
)
BEGIN
    DECLARE @ServiceId64 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId64 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId64, @TenantId, @BranchId, N'ماسك شعر كيراز', 'Kiraz hair mask', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId64, @TenantId, @BranchId, @ServiceId64, 10000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'مساج جسم كامل (ساعة)'
      AND ISNULL(NameEn, '') = 'Full body massage (1 hour)'
)
BEGIN
    DECLARE @ServiceId65 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId65 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId65, @TenantId, @BranchId, N'مساج جسم كامل (ساعة)', 'Full body massage (1 hour)', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId65, @TenantId, @BranchId, @ServiceId65, 20000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'مساج رجل'
      AND ISNULL(NameEn, '') = 'Foot massage'
)
BEGIN
    DECLARE @ServiceId66 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId66 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId66, @TenantId, @BranchId, N'مساج رجل', 'Foot massage', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId66, @TenantId, @BranchId, @ServiceId66, 6000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'مساج رقبة وأكتاف'
      AND ISNULL(NameEn, '') = 'Head and shoulders massage'
)
BEGIN
    DECLARE @ServiceId67 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId67 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId67, @TenantId, @BranchId, N'مساج رقبة وأكتاف', 'Head and shoulders massage', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId67, @TenantId, @BranchId, @ServiceId67, 8000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'مساج ظهر'
      AND ISNULL(NameEn, '') = 'Back massage'
)
BEGIN
    DECLARE @ServiceId68 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId68 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId68, @TenantId, @BranchId, N'مساج ظهر', 'Back massage', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId68, @TenantId, @BranchId, @ServiceId68, 10000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'مساج قدمين'
      AND ISNULL(NameEn, '') = 'Leg massage'
)
BEGIN
    DECLARE @ServiceId69 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId69 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId69, @TenantId, @BranchId, N'مساج قدمين', 'Leg massage', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId69, @TenantId, @BranchId, @ServiceId69, 10000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'مساج يدين'
      AND ISNULL(NameEn, '') = 'Full hand massage'
)
BEGIN
    DECLARE @ServiceId70 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId70 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId70, @TenantId, @BranchId, N'مساج يدين', 'Full hand massage', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId70, @TenantId, @BranchId, @ServiceId70, 6000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'مكياج خاص'
      AND ISNULL(NameEn, '') = 'Special make up'
)
BEGIN
    DECLARE @ServiceId71 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId71 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId71, @TenantId, @BranchId, N'مكياج خاص', 'Special make up', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId71, @TenantId, @BranchId, @ServiceId71, 40000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'مكياج عادي'
      AND ISNULL(NameEn, '') = 'Normal make up'
)
BEGIN
    DECLARE @ServiceId72 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId72 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId72, @TenantId, @BranchId, N'مكياج عادي', 'Normal make up', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId72, @TenantId, @BranchId, @ServiceId72, 25000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'مكياج عيون'
      AND ISNULL(NameEn, '') = 'Eye make up'
)
BEGIN
    DECLARE @ServiceId73 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId73 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId73, @TenantId, @BranchId, N'مكياج عيون', 'Eye make up', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId73, @TenantId, @BranchId, @ServiceId73, 15000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'منيكير'
      AND ISNULL(NameEn, '') = 'Manicure'
)
BEGIN
    DECLARE @ServiceId74 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId74 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId74, @TenantId, @BranchId, N'منيكير', 'Manicure', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId74, @TenantId, @BranchId, @ServiceId74, 6000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'منيكير + بارافين'
      AND ISNULL(NameEn, '') = 'Manicure + Parafin'
)
BEGIN
    DECLARE @ServiceId75 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId75 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId75, @TenantId, @BranchId, N'منيكير + بارافين', 'Manicure + Parafin', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId75, @TenantId, @BranchId, @ServiceId75, 8000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'هايلايت للشعر'
      AND ISNULL(NameEn, '') = 'Highlights hair'
)
BEGIN
    DECLARE @ServiceId76 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId76 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId76, @TenantId, @BranchId, N'هايلايت للشعر', 'Highlights hair', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId76, @TenantId, @BranchId, @ServiceId76, 30000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'واكس البطن'
      AND ISNULL(NameEn, '') = 'Waxing stomach'
)
BEGIN
    DECLARE @ServiceId77 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId77 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId77, @TenantId, @BranchId, N'واكس البطن', 'Waxing stomach', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId77, @TenantId, @BranchId, @ServiceId77, 3500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'واكس البكيني'
      AND ISNULL(NameEn, '') = 'Waxing bikini'
)
BEGIN
    DECLARE @ServiceId78 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId78 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId78, @TenantId, @BranchId, N'واكس البكيني', 'Waxing bikini', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId78, @TenantId, @BranchId, @ServiceId78, 8000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'واكس الظهر'
      AND ISNULL(NameEn, '') = 'Waxing back'
)
BEGIN
    DECLARE @ServiceId79 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId79 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId79, @TenantId, @BranchId, N'واكس الظهر', 'Waxing back', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId79, @TenantId, @BranchId, @ServiceId79, 3500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'واكس تحت الإبط'
      AND ISNULL(NameEn, '') = 'Waxing under arm'
)
BEGIN
    DECLARE @ServiceId80 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId80 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId80, @TenantId, @BranchId, N'واكس تحت الإبط', 'Waxing under arm', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId80, @TenantId, @BranchId, @ServiceId80, 2000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'واكس جسم كامل بدون بكيني'
      AND ISNULL(NameEn, '') = 'Waxing full body without bikini'
)
BEGIN
    DECLARE @ServiceId81 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId81 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId81, @TenantId, @BranchId, N'واكس جسم كامل بدون بكيني', 'Waxing full body without bikini', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId81, @TenantId, @BranchId, @ServiceId81, 25000, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'واكس رجل كاملة'
      AND ISNULL(NameEn, '') = 'Waxing full leg'
)
BEGIN
    DECLARE @ServiceId82 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId82 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId82, @TenantId, @BranchId, N'واكس رجل كاملة', 'Waxing full leg', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId82, @TenantId, @BranchId, @ServiceId82, 6500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'واكس نصف رجل'
      AND ISNULL(NameEn, '') = 'Waxing half leg'
)
BEGIN
    DECLARE @ServiceId83 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId83 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId83, @TenantId, @BranchId, N'واكس نصف رجل', 'Waxing half leg', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId83, @TenantId, @BranchId, @ServiceId83, 3500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'واكس نصف يد'
      AND ISNULL(NameEn, '') = 'Waxing half hand'
)
BEGIN
    DECLARE @ServiceId84 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId84 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId84, @TenantId, @BranchId, N'واكس نصف يد', 'Waxing half hand', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId84, @TenantId, @BranchId, @ServiceId84, 3500, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'واكس يد كاملة'
      AND ISNULL(NameEn, '') = 'Waxing full hand'
)
BEGIN
    DECLARE @ServiceId85 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId85 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId85, @TenantId, @BranchId, N'واكس يد كاملة', 'Waxing full hand', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId85, @TenantId, @BranchId, @ServiceId85, 6600, 'AED', 1, SYSUTCDATETIME());
END

IF NOT EXISTS (
    SELECT 1
    FROM Services
    WHERE TenantId = @TenantId
      AND BranchId = @BranchId
      AND ISNULL(NameAr, N'') = N'ويفي للشعر'
      AND ISNULL(NameEn, '') = 'Wavey hair'
)
BEGIN
    DECLARE @ServiceId86 uniqueidentifier = NEWID();
    DECLARE @ServicePriceId86 uniqueidentifier = NEWID();

    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)
    VALUES (@ServiceId86, @TenantId, @BranchId, N'ويفي للشعر', 'Wavey hair', NULL, 1, SYSUTCDATETIME());

    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)
    VALUES (@ServicePriceId86, @TenantId, @BranchId, @ServiceId86, 15000, 'AED', 1, SYSUTCDATETIME());
END
