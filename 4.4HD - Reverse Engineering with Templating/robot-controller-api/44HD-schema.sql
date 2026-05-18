--
-- PostgreSQL database dump
--

\restrict 3ncXT0dXuZed5WRh0AUsOcHBi6HuGIZI3Gp8gXgRJbfKYeuPBj6FHJqyEWPMv2R

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: map; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.map (
    id integer NOT NULL,
    name character varying(100) NOT NULL,
    description character varying(800),
    rows integer NOT NULL,
    columns integer NOT NULL,
    issquare boolean GENERATED ALWAYS AS (((rows > 0) AND (rows = columns))) STORED,
    createddate timestamp without time zone NOT NULL,
    modifieddate timestamp without time zone NOT NULL
);


--
-- Name: map_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.map ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.map_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: robotcommand; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.robotcommand (
    id integer NOT NULL,
    "Name" character varying(50) NOT NULL,
    description character varying(800),
    ismovecommand boolean NOT NULL,
    createddate timestamp without time zone NOT NULL,
    modifieddate timestamp without time zone NOT NULL
);


--
-- Name: robotcommand_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.robotcommand ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.robotcommand_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: user; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."user" (
    id integer NOT NULL,
    email text NOT NULL,
    firstname text NOT NULL,
    lastname text NOT NULL,
    passwordhash text NOT NULL,
    description text,
    role text DEFAULT 'User'::text NOT NULL,
    createddate timestamp without time zone NOT NULL,
    modifieddate timestamp without time zone NOT NULL
);


--
-- Name: TABLE "user"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public."user" IS 'Application users. Passwords must be stored only as secure hashes; do not store plaintext.';


--
-- Name: COLUMN "user".passwordhash; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."user".passwordhash IS 'ASP.NET Identity or other secure password hash; application must verify using appropriate hasher.';


--
-- Name: user_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.user_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: user_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.user_id_seq OWNED BY public."user".id;


--
-- Name: user id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."user" ALTER COLUMN id SET DEFAULT nextval('public.user_id_seq'::regclass);


--
-- Name: map map_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.map
    ADD CONSTRAINT map_pkey PRIMARY KEY (id);


--
-- Name: robotcommand robotcommand_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.robotcommand
    ADD CONSTRAINT robotcommand_pkey PRIMARY KEY (id);


--
-- Name: user user_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_pkey PRIMARY KEY (id);


--
-- Name: ux_robotcommand_name_ci; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_robotcommand_name_ci ON public.robotcommand USING btree (lower(("Name")::text));


--
-- Name: ux_user_email_lower; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_user_email_lower ON public."user" USING btree (lower(email));


--
-- PostgreSQL database dump complete
--

\unrestrict 3ncXT0dXuZed5WRh0AUsOcHBi6HuGIZI3Gp8gXgRJbfKYeuPBj6FHJqyEWPMv2R

