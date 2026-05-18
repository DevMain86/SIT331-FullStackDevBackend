--
-- PostgreSQL database dump
--

\restrict TTmqpi4ke9EC3e64bLM33LtyCzUjNpL6AgBpj1KabaQaEyA1I2rX8R6h0dyiRBo

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
-- Name: map; Type: TABLE; Schema: public; Owner: postgres
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


ALTER TABLE public.map OWNER TO postgres;

--
-- Name: map_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
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
-- Name: robotcommand; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.robotcommand (
    id integer NOT NULL,
    "Name" character varying(50) NOT NULL,
    description character varying(800),
    ismovecommand boolean NOT NULL,
    createddate timestamp without time zone NOT NULL,
    modifieddate timestamp without time zone NOT NULL
);


ALTER TABLE public.robotcommand OWNER TO postgres;

--
-- Name: robotcommand_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
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
-- Name: map map_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.map
    ADD CONSTRAINT map_pkey PRIMARY KEY (id);


--
-- Name: robotcommand robotcommand_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.robotcommand
    ADD CONSTRAINT robotcommand_pkey PRIMARY KEY (id);


--
-- Name: ux_robotcommand_name_ci; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX ux_robotcommand_name_ci ON public.robotcommand USING btree (lower(("Name")::text));


--
-- PostgreSQL database dump complete
--

\unrestrict TTmqpi4ke9EC3e64bLM33LtyCzUjNpL6AgBpj1KabaQaEyA1I2rX8R6h0dyiRBo

