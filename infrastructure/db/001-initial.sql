BEGIN;
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE TABLE IF NOT EXISTS source_snapshots (name text PRIMARY KEY, snapshot jsonb NOT NULL);
CREATE TABLE IF NOT EXISTS anonymous_sessions (
  token_hash text PRIMARY KEY,
  expires_at timestamptz NOT NULL
);
CREATE TABLE IF NOT EXISTS assessments (
  trip_id uuid NOT NULL,
  version integer NOT NULL CHECK (version > 0),
  owner_hash text NOT NULL REFERENCES anonymous_sessions(token_hash) ON DELETE CASCADE,
  snapshot jsonb NOT NULL,
  expires_at timestamptz NOT NULL,
  route geometry(LineString,4326) NOT NULL,
  PRIMARY KEY (trip_id,version)
);
CREATE INDEX IF NOT EXISTS assessments_owner_idx ON assessments(owner_hash,trip_id,version DESC);
CREATE INDEX IF NOT EXISTS assessments_expiry_idx ON assessments(expires_at);
CREATE INDEX IF NOT EXISTS assessments_route_idx ON assessments USING gist(route);
CREATE TABLE IF NOT EXISTS idempotency (
  owner_hash text NOT NULL,
  key text NOT NULL,
  body_hash text NOT NULL,
  trip_id uuid NOT NULL,
  version integer NOT NULL,
  PRIMARY KEY(owner_hash,key),
  FOREIGN KEY(trip_id,version) REFERENCES assessments(trip_id,version) ON DELETE CASCADE
);
COMMIT;
