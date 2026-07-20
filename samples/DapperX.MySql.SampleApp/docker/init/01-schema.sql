CREATE TABLE IF NOT EXISTS students (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(255) NOT NULL
);

INSERT INTO students (name) VALUES
  ('Alice'),
  ('Bob'),
  ('Carol');
