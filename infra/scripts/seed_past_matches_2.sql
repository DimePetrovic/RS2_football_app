DO $$
DECLARE
    org_id UUID := 'ba3b9b3d-ead5-4aea-9b88-729e4710fddc';
    m_id UUID;
    match_data RECORD;
BEGIN
    FOR match_data IN SELECT * FROM (VALUES
        ('Jutarnja liga - Kolo 1',     '2026-05-05 09:00:00+00'::TIMESTAMPTZ, 'Sportski centar Banjica'),
        ('Jutarnja liga - Kolo 2',     '2026-05-08 09:00:00+00'::TIMESTAMPTZ, 'Sportski centar Banjica'),
        ('Vikend turnir - Kolo 1',     '2026-05-11 11:00:00+00'::TIMESTAMPTZ, 'SC Olimp'),
        ('Vikend turnir - Kolo 2',     '2026-05-13 11:00:00+00'::TIMESTAMPTZ, 'SC Olimp'),
        ('Mali fudbal petkom',         '2026-05-16 19:30:00+00'::TIMESTAMPTZ, 'Kosutnjak - teren 3'),
        ('Druzenje uz lopticu',        '2026-05-19 18:00:00+00'::TIMESTAMPTZ, 'Kosutnjak - teren 3'),
        ('Lokalni derbi',              '2026-05-22 20:00:00+00'::TIMESTAMPTZ, 'Sportski centar Banjica'),
        ('Prijateljska revanš utakmica','2026-05-26 17:30:00+00'::TIMESTAMPTZ, 'SC Olimp'),
        ('Noćni mec',                  '2026-05-29 21:00:00+00'::TIMESTAMPTZ, 'Sportski centar Banjica'),
        ('Zagrevanje za sezonu',       '2026-06-02 18:30:00+00'::TIMESTAMPTZ, 'Kosutnjak - teren 3')
    ) AS t(title, starts_at, location) LOOP

        m_id := gen_random_uuid();

        INSERT INTO matches ("Id","Title","Type","Status","OrganizerUserId","Location","StartsAt","DurationMinutes","PlayersPerTeam","MaxSubstitutes","CreatedAt")
        VALUES (m_id, match_data.title, 'Independent', 'Scheduled', org_id, match_data.location, match_data.starts_at, 90, 4, 1, '2026-04-20 10:00:00+00');

        -- fani (organizer) - Home, kapiten
        INSERT INTO match_participants ("Id","MatchId","UserId","DisplayName","IsOrganizer","Status","InvitedAt","RespondedAt","IsCaptain","Team")
        VALUES (gen_random_uuid(), m_id, 'ba3b9b3d-ead5-4aea-9b88-729e4710fddc', 'fani', true,  'Accepted', '2026-04-20 10:00:00+00', '2026-04-20 10:00:00+00', true,  'Home');

        -- ssss - Home
        INSERT INTO match_participants ("Id","MatchId","UserId","DisplayName","IsOrganizer","Status","InvitedAt","RespondedAt","IsCaptain","Team")
        VALUES (gen_random_uuid(), m_id, '78845b3a-cb8b-4364-844b-2de327af5376', 'ssss', false, 'Accepted', '2026-04-20 10:00:00+00', '2026-04-20 10:05:00+00', false, 'Home');

        -- gzuz - Home
        INSERT INTO match_participants ("Id","MatchId","UserId","DisplayName","IsOrganizer","Status","InvitedAt","RespondedAt","IsCaptain","Team")
        VALUES (gen_random_uuid(), m_id, '2b130fe0-1e59-4113-b5fb-266088a94ef1', 'gzuz', false, 'Accepted', '2026-04-20 10:00:00+00', '2026-04-20 10:06:00+00', false, 'Home');

        -- d73 - Home
        INSERT INTO match_participants ("Id","MatchId","UserId","DisplayName","IsOrganizer","Status","InvitedAt","RespondedAt","IsCaptain","Team")
        VALUES (gen_random_uuid(), m_id, 'a908c063-484d-4003-a0ea-0d7454070ffb', 'd73', false, 'Accepted', '2026-04-20 10:00:00+00', '2026-04-20 10:07:00+00', false, 'Home');

        -- dimee62 - Away, kapiten
        INSERT INTO match_participants ("Id","MatchId","UserId","DisplayName","IsOrganizer","Status","InvitedAt","RespondedAt","IsCaptain","Team")
        VALUES (gen_random_uuid(), m_id, '6f0f22de-f180-4bb6-97a0-e7f9b7948315', 'dimee62', false, 'Accepted', '2026-04-20 10:00:00+00', '2026-04-20 10:08:00+00', true,  'Away');

        -- neko - Away
        INSERT INTO match_participants ("Id","MatchId","UserId","DisplayName","IsOrganizer","Status","InvitedAt","RespondedAt","IsCaptain","Team")
        VALUES (gen_random_uuid(), m_id, 'a03b7870-ec30-4c3f-964c-cea40d16ce3a', 'neko', false, 'Accepted', '2026-04-20 10:00:00+00', '2026-04-20 10:09:00+00', false, 'Away');

        -- admin - Away
        INSERT INTO match_participants ("Id","MatchId","UserId","DisplayName","IsOrganizer","Status","InvitedAt","RespondedAt","IsCaptain","Team")
        VALUES (gen_random_uuid(), m_id, '11111111-1111-1111-1111-111111111111', 'admin', false, 'Accepted', '2026-04-20 10:00:00+00', '2026-04-20 10:10:00+00', false, 'Away');

        -- camm - Away
        INSERT INTO match_participants ("Id","MatchId","UserId","DisplayName","IsOrganizer","Status","InvitedAt","RespondedAt","IsCaptain","Team")
        VALUES (gen_random_uuid(), m_id, '53090798-c2dd-45f5-9c47-8d68e7644afc', 'camm', false, 'Accepted', '2026-04-20 10:00:00+00', '2026-04-20 10:11:00+00', false, 'Away');

        -- vlucic - zamena
        INSERT INTO match_participants ("Id","MatchId","UserId","DisplayName","IsOrganizer","Status","InvitedAt","RespondedAt","IsCaptain","Team")
        VALUES (gen_random_uuid(), m_id, '6a123ce4-415b-4803-a0cc-591a3250a4ba', 'vlucic', false, 'Accepted', '2026-04-20 10:00:00+00', '2026-04-20 10:12:00+00', false, 'None');

        RAISE NOTICE 'Kreiran mec: %', match_data.title;
    END LOOP;
END $$;
