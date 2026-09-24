modelBuilder.Entity("Hospital.Domain.Entities.Bed", b =>
    {
        b.Property<Guid>("Id")
            .ValueGeneratedOnAdd()
            .HasColumnType("uuid");

        b.Property<string>("BedLabel")
            .IsRequired()
            .HasMaxLength(16)
            .HasColumnType("character varying(16)");

        b.Property<DateTimeOffset>("CreatedAt")
            .HasColumnType("timestamp with time zone");

        b.Property<bool>("IsOccupied")
            .HasColumnType("boolean");

        b.Property<DateTimeOffset>("UpdatedAt")
            .HasColumnType("timestamp with time zone");

        b.Property<Guid>("WardId")
            .HasColumnType("uuid");

        b.HasKey("Id");

        b.HasIndex("WardId", "BedLabel")
            .IsUnique();

        b.ToTable("beds", (string)null);
    });

modelBuilder.Entity("Hospital.Domain.Entities.Complaint", b =>
    {
        b.Property<Guid>("Id")
            .ValueGeneratedOnAdd()
            .HasColumnType("uuid");

        b.Property<Guid?>("AssignedTo")
            .HasColumnType("uuid");

        b.Property<DateTimeOffset>("CreatedAt")
            .HasColumnType("timestamp with time zone");

        b.Property<string>("Description")
            .IsRequired()
            .HasMaxLength(2000)
            .HasColumnType("character varying(2000)");

        b.Property<DateTimeOffset?>("EscalatedAt")
            .HasColumnType("timestamp with time zone");

        b.Property<Guid?>("FeedbackId")
            .HasColumnType("uuid");

        b.Property<Guid>("PatientId")
            .HasColumnType("uuid");

        b.Property<int>("Priority")
            .HasColumnType("integer");

        b.Property<int>("Status")
            .HasColumnType("integer");

        b.Property<string>("Subject")
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("character varying(200)");

        b.Property<DateTimeOffset>("UpdatedAt")
            .HasColumnType("timestamp with time zone");

        b.HasKey("Id");

        b.HasIndex("AssignedTo");

        b.HasIndex("CreatedAt");

        b.HasIndex("FeedbackId");

        b.HasIndex("PatientId");

        b.HasIndex("Status");

        b.ToTable("complaints", (string)null);
    });